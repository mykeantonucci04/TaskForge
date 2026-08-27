namespace TaskForge.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserEmail { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}

public interface ICurrentTenantService
{
    Guid? TenantId { get; }
    string SchemaName { get; }
    bool IsResolved { get; }
}

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
}

public interface IStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> DownloadFileAsync(string storagePath, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string storagePath, CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task SendNotificationToUserAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default);
}

public interface IJwtTokenGenerator
{
    (string AccessToken, string RefreshToken, DateTime RefreshTokenExpiryUtc) GenerateTokens(Guid userId, string email, string role, Guid tenantId, string schemaName);
}
