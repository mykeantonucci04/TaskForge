using FluentValidation;
using Microsoft.Extensions.Logging;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Application.Common.Messaging;
using TaskForge.Domain.Common;
using TaskForge.Domain.Interfaces;
using TaskForge.Shared.Exceptions;
using TaskForge.Shared.Results;

namespace TaskForge.Application.Features.Auth;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime RefreshTokenExpiryUtc,
    UserDto User);

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string Role,
    Guid TenantId,
    string SchemaName);

public record LoginCommand(string Email, string Password, Guid? TenantId = null) : ICommand<AuthResponseDto>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class LoginCommandHandler(
    IUserRepository userRepository,
    ITenantMembershipRepository membershipRepository,
    ITenantRepository tenantRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork,
    ILogger<LoginCommandHandler>? logger = null) : ICommandHandler<LoginCommand, AuthResponseDto>
{
    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!Email.TryCreate(request.Email, out var emailVo) || emailVo is null)
            {
                return Result.Failure<AuthResponseDto>(
                    Error.Unauthorized("Auth.InvalidCredentials", "Credenziali non valide o email non registrata."));
            }

            var user = await userRepository.GetByEmailAsync(emailVo, cancellationToken);

            if (user is null || !user.IsActive)
            {
                return Result.Failure<AuthResponseDto>(
                    Error.Unauthorized("Auth.InvalidCredentials", "Credenziali non valide o email non registrata."));
            }

            if (!passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Result.Failure<AuthResponseDto>(
                    Error.Unauthorized("Auth.InvalidCredentials", "Credenziali non valide o email non registrata."));
            }

            var memberships = await membershipRepository.GetMembershipsByUserAsync(user.Id, cancellationToken);
            var activeMemberships = memberships.Where(m => m.Status == Domain.Enums.MembershipStatus.Active).ToList();

            if (activeMemberships.Count == 0)
            {
                return Result.Failure<AuthResponseDto>(
                    Error.Unauthorized("Auth.NoActiveMembership", "User has no active organization memberships."));
            }

            var selectedMembership = request.TenantId.HasValue
                ? activeMemberships.FirstOrDefault(m => m.TenantId == request.TenantId.Value)
                : activeMemberships.First();

            if (selectedMembership is null)
            {
                return Result.Failure<AuthResponseDto>(
                    Error.Unauthorized("Auth.InvalidTenant", "User is not a member of the requested organization."));
            }

            var tenant = selectedMembership.Tenant ?? await tenantRepository.GetByIdAsync(selectedMembership.TenantId, cancellationToken);
            var schemaName = tenant?.SchemaName ?? $"tenant_{selectedMembership.TenantId:N}";

            var (accessToken, refreshToken, expiry) = tokenGenerator.GenerateTokens(
                user.Id,
                user.Email.Value,
                selectedMembership.Role.ToString(),
                selectedMembership.TenantId,
                schemaName);

            try
            {
                user.AddRefreshToken(refreshToken, expiry);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Could not persist refresh token for user {UserId}. Access token remains valid.", user.Id);
            }

            var userDto = new UserDto(
                user.Id,
                user.Email.Value,
                user.FirstName,
                user.LastName,
                user.FullName,
                selectedMembership.Role.ToString(),
                selectedMembership.TenantId,
                schemaName);

            return Result.Success(new AuthResponseDto(accessToken, refreshToken, expiry, userDto));
        }
        catch (DomainException ex)
        {
            logger?.LogWarning(ex, "Domain exception during login for {Email}: {Message}", request.Email, ex.Message);
            return Result.Failure<AuthResponseDto>(
                Error.Unauthorized("Auth.InvalidCredentials", ex.Message));
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Unexpected exception during login for {Email}", request.Email);
            return Result.Failure<AuthResponseDto>(
                Error.Unauthorized("Auth.LoginError", "Si è verificato un errore durante l'accesso. Riprova più tardi."));
        }
    }
}

