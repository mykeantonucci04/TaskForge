using FluentValidation;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Application.Common.Messaging;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Interfaces;

namespace TaskForge.Application.Features.Auth;

public record AuthResponseDto(string AccessToken, string RefreshToken, DateTime RefreshTokenExpiryUtc, UserDto User);

public record UserDto(Guid Id, string Email, string FirstName, string LastName, string Role, Guid TenantId);

public record LoginCommand(string Email, string Password) : ICommand<AuthResponseDto>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}

public class LoginCommandHandler(
    IRepository<User> userRepository,
    IJwtTokenGenerator tokenGenerator) : ICommandHandler<LoginCommand, AuthResponseDto>
{
    public async Task<Shared.Results.Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var users = await userRepository.FindAsync(u => u.Email == request.Email, cancellationToken);
        var user = users.FirstOrDefault();

        if (user is null || !user.IsActive)
        {
            return Shared.Results.Result.Failure<AuthResponseDto>(
                Shared.Results.Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
        }

        // Note: In real app, verify BCrypt/Argon2 password hash
        var (accessToken, refreshToken, expiry) = tokenGenerator.GenerateTokens(
            user.Id, user.Email, user.Role.ToString(), user.TenantId, "tenant_" + user.TenantId.ToString("N"));

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryUtc = expiry;
        userRepository.Update(user);

        var userDto = new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.Role.ToString(), user.TenantId);
        return new AuthResponseDto(accessToken, refreshToken, expiry, userDto);
    }
}
