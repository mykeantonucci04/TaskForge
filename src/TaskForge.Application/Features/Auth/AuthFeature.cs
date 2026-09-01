using FluentValidation;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Application.Common.Messaging;
using TaskForge.Domain.Common;
using TaskForge.Domain.Interfaces;
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
    IUnitOfWork unitOfWork) : ICommandHandler<LoginCommand, AuthResponseDto>
{
    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var emailVo = Email.Create(request.Email);
        var user = await userRepository.GetByEmailAsync(emailVo, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthResponseDto>(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
        }

        if (!passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            return Result.Failure<AuthResponseDto>(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
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

        var tenant = await tenantRepository.GetByIdAsync(selectedMembership.TenantId, cancellationToken);
        var schemaName = tenant?.SchemaName ?? $"tenant_{selectedMembership.TenantId:N}";

        var (accessToken, refreshToken, expiry) = tokenGenerator.GenerateTokens(
            user.Id,
            user.Email.Value,
            selectedMembership.Role.ToString(),
            selectedMembership.TenantId,
            schemaName);

        user.AddRefreshToken(refreshToken, expiry);
        await unitOfWork.SaveChangesAsync(cancellationToken);

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
}
