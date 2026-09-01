using FluentValidation;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Application.Common.Messaging;
using TaskForge.Domain.Common;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Enums;
using TaskForge.Domain.Interfaces;
using TaskForge.Shared.Results;

namespace TaskForge.Application.Features.Tenants;

public record RegisterTenantCommand(
    string CompanyName,
    string Slug,
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string AdminPassword,
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
            .NotEmpty().WithMessage("Admin first name is required.")
            .MaximumLength(100).WithMessage("Admin first name cannot exceed 100 characters.");

        RuleFor(x => x.AdminLastName)
            .NotEmpty().WithMessage("Admin last name is required.")
            .MaximumLength(100).WithMessage("Admin last name cannot exceed 100 characters.");

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("Admin email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.");

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("Admin password is required.")
            .MinimumLength(8).WithMessage("Admin password must be at least 8 characters.");
    }
}

public class RegisterTenantCommandHandler(
    ITenantRepository tenantRepository,
    IUserRepository userRepository,
    ITenantMembershipRepository membershipRepository,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ISchemaProvisioner schemaProvisioner,
    IJwtTokenGenerator tokenGenerator) : ICommandHandler<RegisterTenantCommand, RegisterTenantResponseDto>
{
    public async Task<Result<RegisterTenantResponseDto>> Handle(RegisterTenantCommand request, CancellationToken cancellationToken)
    {
        var slugVo = TenantSlug.Create(request.Slug);
        var emailVo = Email.Create(request.AdminEmail);

        // 1. Check if slug already exists
        var slugExists = await tenantRepository.ExistsBySlugAsync(slugVo, cancellationToken);
        if (slugExists)
        {
            return Result.Failure<RegisterTenantResponseDto>(
                Error.Conflict("Tenant.SlugExists", $"The organization URL identifier '{request.Slug}' is already taken."));
        }

        // 2. Get or create User
        var user = await userRepository.GetByEmailAsync(emailVo, cancellationToken);
        var isNewUser = false;
        if (user is null)
        {
            isNewUser = true;
            var passwordHash = passwordHasher.HashPassword(request.AdminPassword);
            user = User.Create(emailVo, passwordHash, request.AdminFirstName, request.AdminLastName);
        }

        // 3. Create Tenant
        var tenant = Tenant.Create(request.CompanyName, slugVo, request.Plan, user.Email.Value);

        // 4. Create TenantMembership as Owner
        var membership = TenantMembership.Create(tenant.Id, user.Id, MembershipRole.Owner, MembershipStatus.Active, user.Email.Value);

        // 5. Issue JWT Tokens for the new Owner scoped to this tenant
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

        // 6. Persist public metadata
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // 7. Provision dedicated PostgreSQL schema for tenant
        await schemaProvisioner.ProvisionTenantSchemaAsync(tenant.SchemaName, cancellationToken);

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

        return Result.Success(response);
    }
}
