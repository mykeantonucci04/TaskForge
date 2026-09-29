using FluentValidation;
using Microsoft.Extensions.Logging;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Application.Common.Messaging;
using TaskForge.Domain.Common;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Enums;
using TaskForge.Domain.Interfaces;
using TaskForge.Shared.Exceptions;
using TaskForge.Shared.Results;

namespace TaskForge.Application.Features.Tenants;

public record RegisterTenantCommand(
    string CompanyName,
    string Slug,
    string AdminFirstName = "Admin",
    string AdminLastName = "Admin",
    string AdminEmail = "",
    string AdminPassword = "",
    TenantPlan Plan = TenantPlan.Free) : ICommand<RegisterTenantResponseDto>;

public record RegisterTenantResponseDto(
    Guid TenantId,
    string CompanyName,
    string Slug,
    string SchemaName,
    TenantPlan Plan,
    Guid AdminUserId,
    string AdminEmail,
    string AdminFullName,
    string Role,
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiryUtc);

public class RegisterTenantCommandValidator : AbstractValidator<RegisterTenantCommand>
{
    public RegisterTenantCommandValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(100).WithMessage("Company name cannot exceed 100 characters.");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Tenant slug is required.")
            .MinimumLength(3).WithMessage("Tenant slug must be at least 3 characters.")
            .MaximumLength(50).WithMessage("Tenant slug cannot exceed 50 characters.")
            .Matches(@"^[a-z0-9]+(-[a-z0-9]+)*$")
            .WithMessage("Tenant slug can only contain lowercase letters, numbers, and non-consecutive hyphens.");

        RuleFor(x => x.AdminFirstName)
            .MaximumLength(100).WithMessage("Admin first name cannot exceed 100 characters.");

        RuleFor(x => x.AdminLastName)
            .MaximumLength(100).WithMessage("Admin last name cannot exceed 100 characters.");

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("Admin email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("Admin password is required.")
            .MinimumLength(8).WithMessage("Admin password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches(@"[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]").WithMessage("Password must contain at least one special character.");
    }
}

public class RegisterTenantCommandHandler(
    ITenantRepository tenantRepository,
    IUserRepository userRepository,
    ITenantMembershipRepository membershipRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ISchemaProvisioner schemaProvisioner,
    IJwtTokenGenerator tokenGenerator,
    ILogger<RegisterTenantCommandHandler> logger) : ICommandHandler<RegisterTenantCommand, RegisterTenantResponseDto>
{
    public async Task<Result<RegisterTenantResponseDto>> Handle(RegisterTenantCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate Value Objects with safe error handling
        TenantSlug slugVo;
        try
        {
            slugVo = TenantSlug.Create(request.Slug);
        }
        catch (DomainException ex)
        {
            logger.LogWarning("Registration failed - invalid slug '{Slug}': {Message}", request.Slug, ex.Message);
            return Result.Failure<RegisterTenantResponseDto>(
                Error.Validation("Tenant.InvalidSlug", ex.Message));
        }

        Email emailVo;
        try
        {
            emailVo = Email.Create(request.AdminEmail);
        }
        catch (DomainException ex)
        {
            logger.LogWarning("Registration failed - invalid email '{Email}': {Message}", request.AdminEmail, ex.Message);
            return Result.Failure<RegisterTenantResponseDto>(
                Error.Validation("Tenant.InvalidEmail", ex.Message));
        }

        try
        {
            // 2. Check if slug already exists
            var slugExists = await tenantRepository.ExistsBySlugAsync(slugVo, cancellationToken);
            if (slugExists)
            {
                logger.LogWarning("Registration failed - slug '{Slug}' already taken", request.Slug);
                return Result.Failure<RegisterTenantResponseDto>(
                    Error.Conflict("Tenant.SlugExists", $"The organization URL identifier '{request.Slug}' is already taken."));
            }

            // 3. Get or create User
            var user = await userRepository.GetByEmailAsync(emailVo, cancellationToken);
            var isNewUser = false;
            if (user is null)
            {
                isNewUser = true;
                var passwordHash = passwordHasher.HashPassword(request.AdminPassword);
                var adminFirstName = string.IsNullOrWhiteSpace(request.AdminFirstName) ? "Admin" : request.AdminFirstName.Trim();
                var adminLastName = string.IsNullOrWhiteSpace(request.AdminLastName) ? "Admin" : request.AdminLastName.Trim();

                try
                {
                    user = User.Create(emailVo, passwordHash, adminFirstName, adminLastName);
                }
                catch (DomainException ex)
                {
                    logger.LogWarning("Registration failed - user creation error: {Message}", ex.Message);
                    return Result.Failure<RegisterTenantResponseDto>(
                        Error.Validation("Tenant.UserCreation", ex.Message));
                }
            }

            // 4. Create Tenant
            Tenant tenant;
            try
            {
                tenant = Tenant.Create(request.CompanyName, slugVo, request.Plan, user.Email.Value);
            }
            catch (DomainException ex)
            {
                logger.LogWarning("Registration failed - tenant creation error: {Message}", ex.Message);
                return Result.Failure<RegisterTenantResponseDto>(
                    Error.Validation("Tenant.CreationFailed", ex.Message));
            }

            // 5. Create TenantMembership as Owner
            var membership = TenantMembership.Create(tenant.Id, user.Id, MembershipRole.Owner, MembershipStatus.Active, user.Email.Value);

            // 6. Issue JWT Tokens for the new Owner scoped to this tenant
            var (accessToken, refreshTokenStr, refreshTokenExpiry) = tokenGenerator.GenerateTokens(
                user.Id,
                user.Email.Value,
                MembershipRole.Owner.ToString(),
                tenant.Id,
                tenant.SchemaName);

            user.AddRefreshToken(refreshTokenStr, refreshTokenExpiry);

            if (isNewUser)
            {
                await userRepository.AddAsync(user, cancellationToken);
            }
            else
            {
                userRepository.Update(user);
            }

            await tenantRepository.AddAsync(tenant, cancellationToken);
            await membershipRepository.AddAsync(membership, cancellationToken);

            // 7. Persist public metadata
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // 8. Provision dedicated PostgreSQL schema for tenant
            try
            {
                await schemaProvisioner.ProvisionTenantSchemaAsync(tenant.SchemaName, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Schema provisioning failed for tenant '{SchemaName}'. Data was persisted but schema may need manual provisioning.", tenant.SchemaName);
                // Data is saved but schema provisioning failed - still return success
                // because the tenant record exists. Schema can be re-provisioned later.
            }

            var response = new RegisterTenantResponseDto(
                tenant.Id,
                tenant.Name,
                tenant.Slug.Value,
                tenant.SchemaName,
                tenant.Plan,
                user.Id,
                user.Email.Value,
                user.FullName,
                MembershipRole.Owner.ToString(),
                accessToken,
                refreshTokenStr,
                refreshTokenExpiry);

            logger.LogInformation("Successfully registered tenant '{TenantName}' (schema: {SchemaName}) with admin '{Email}'",
                tenant.Name, tenant.SchemaName, user.Email.Value);

            return Result.Success(response);
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "Registration failed with domain error: {Message}", ex.Message);
            return Result.Failure<RegisterTenantResponseDto>(
                Error.Validation("Tenant.DomainError", ex.Message));
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateException" || ex.GetType().BaseType?.Name == "DbUpdateException")
        {
            logger.LogError(ex, "Registration failed with database error");

            // Check for unique constraint violations
            var innerMessage = ex.InnerException?.Message ?? ex.Message;
            if (innerMessage.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
                innerMessage.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) ||
                innerMessage.Contains("23505", StringComparison.Ordinal)) // PostgreSQL unique violation code
            {
                if (innerMessage.Contains("email", StringComparison.OrdinalIgnoreCase))
                {
                    return Result.Failure<RegisterTenantResponseDto>(
                        Error.Conflict("Tenant.EmailExists", "An account with this email address already exists."));
                }
                if (innerMessage.Contains("slug", StringComparison.OrdinalIgnoreCase))
                {
                    return Result.Failure<RegisterTenantResponseDto>(
                        Error.Conflict("Tenant.SlugExists", $"The organization URL identifier '{request.Slug}' is already taken."));
                }
                return Result.Failure<RegisterTenantResponseDto>(
                    Error.Conflict("Tenant.DuplicateRecord", "A record with these details already exists."));
            }

            return Result.Failure<RegisterTenantResponseDto>(
                Error.Validation("Tenant.DatabaseError", "An error occurred while saving the registration data. Please try again."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during tenant registration for '{CompanyName}'", request.CompanyName);
            return Result.Failure<RegisterTenantResponseDto>(
                Error.Validation("Tenant.UnexpectedError", "An unexpected error occurred during registration. Please try again later."));
        }
    }
}
