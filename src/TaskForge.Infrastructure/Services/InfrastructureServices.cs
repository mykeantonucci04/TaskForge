using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TaskForge.Application.Common.Interfaces;
using TaskForge.Domain.Interfaces;
using TaskForge.Shared.Constants;

namespace TaskForge.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public class CurrentTenantService(ITenantContext tenantContext) : ICurrentTenantService
{
    public Guid? TenantId => tenantContext.TenantId;
    public string SchemaName => tenantContext.SchemaName;
    public bool IsResolved => tenantContext.IsResolved;
}

public class JwtTokenGenerator(IConfiguration configuration) : IJwtTokenGenerator
{
    public (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiryUtc) GenerateTokens(
        Guid userId, string email, string role, Guid tenantId, string schemaName)
    {
        var secretKey = configuration["JwtSettings:SecretKey"] ?? "SuperSecretKeyForTaskForgeBackend12345!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(TenantConstants.ClaimName, tenantId.ToString()),
            new Claim(TenantConstants.SchemaClaimName, schemaName)
        };

        var token = new JwtSecurityToken(
            issuer: configuration["JwtSettings:Issuer"] ?? "TaskForgeApi",
            audience: configuration["JwtSettings:Audience"] ?? "TaskForgeClient",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var expiry = DateTime.UtcNow.AddDays(7);

        return (accessToken, refreshToken, expiry);
    }
}

public class NotificationHub : Hub
{
    public async Task JoinTenantGroup(string tenantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
    }
}

public class SignalRNotificationService(IHubContext<NotificationHub> hubContext) : INotificationService
{
    public async Task SendNotificationToUserAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default)
    {
        await hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new { title, message }, cancellationToken);
    }
}

public class S3StorageService(ILogger<S3StorageService> logger) : IStorageService
{
    public Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Uploading attachment {FileName} to S3 bucket", fileName);
        var storagePath = $"attachments/{Guid.NewGuid():N}_{fileName}";
        return Task.FromResult(storagePath);
    }

    public Task<Stream> DownloadFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Downloading file from S3: {StoragePath}", storagePath);
        return Task.FromResult<Stream>(new MemoryStream());
    }

    public Task DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting file from S3: {StoragePath}", storagePath);
        return Task.CompletedTask;
    }
}

public class SmtpEmailService(ILogger<SmtpEmailService> logger) : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Sending email to {To} with subject: {Subject}", to, subject);
        return Task.CompletedTask;
    }
}
