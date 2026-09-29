using TaskForge.Domain.Common;
using TaskForge.Shared.Exceptions;

namespace TaskForge.Domain.Entities;

public class RefreshToken : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public string? CreatedByIp { get; private set; }
    public DateTime? RevokedAtUtc { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public User User { get; private set; } = null!;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    // Private constructor for EF Core
    private RefreshToken() { }

    private RefreshToken(Guid id, Guid userId, string token, DateTime expiresAtUtc, string? createdByIp)
        : base(id)
    {
        UserId = userId;
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
        CreatedByIp = createdByIp;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static RefreshToken Create(Guid userId, string token, DateTime expiresAtUtc, string? createdByIp = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User ID cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new DomainException("Refresh token cannot be empty.");
        }

        if (expiresAtUtc <= DateTime.UtcNow)
        {
            throw new DomainException("Expiration time must be in the future.");
        }

        return new RefreshToken(Guid.Empty, userId, token, expiresAtUtc, createdByIp);
    }

    public void Revoke(string? revokedByIp = null, string? replacedByToken = null)
    {
        RevokedAtUtc = DateTime.UtcNow;
        RevokedByIp = revokedByIp;
        ReplacedByToken = replacedByToken;
    }
}
