using TaskForge.Domain.Common;
using TaskForge.Shared.Exceptions;

namespace TaskForge.Domain.Entities;

public class User : AggregateRoot<Guid>, IAuditableEntity
{
    public const int MaxNameLength = 100;

    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    public ICollection<TenantMembership> Memberships { get; private set; } = new List<TenantMembership>();
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    public string FullName => $"{FirstName} {LastName}".Trim();

    // Private constructor for EF Core
    private User() { }

    private User(Guid id, Email email, string passwordHash, string firstName, string lastName, string? createdBy)
        : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        IsActive = true;
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    public static User Create(Email email, string passwordHash, string firstName, string lastName, string? createdBy = null)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new DomainException("First name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new DomainException("Last name cannot be empty.");
        }

        var trimmedFirst = firstName.Trim();
        var trimmedLast = lastName.Trim();

        if (trimmedFirst.Length > MaxNameLength)
        {
            throw new DomainException($"First name cannot exceed {MaxNameLength} characters.");
        }

        if (trimmedLast.Length > MaxNameLength)
        {
            throw new DomainException($"Last name cannot exceed {MaxNameLength} characters.");
        }

        var userId = Guid.NewGuid();
        return new User(userId, email, passwordHash, trimmedFirst, trimmedLast, createdBy);
    }

    public void UpdateProfile(string firstName, string lastName, string? modifiedBy = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new DomainException("First name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new DomainException("Last name cannot be empty.");
        }

        var trimmedFirst = firstName.Trim();
        var trimmedLast = lastName.Trim();

        if (trimmedFirst.Length > MaxNameLength)
        {
            throw new DomainException($"First name cannot exceed {MaxNameLength} characters.");
        }

        if (trimmedLast.Length > MaxNameLength)
        {
            throw new DomainException($"Last name cannot exceed {MaxNameLength} characters.");
        }

        FirstName = trimmedFirst;
        LastName = trimmedLast;
        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void ChangePassword(string newPasswordHash, string? modifiedBy = null)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new DomainException("New password hash cannot be empty.");
        }

        PasswordHash = newPasswordHash;
        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void Deactivate(string? modifiedBy = null)
    {
        IsActive = false;
        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void Activate(string? modifiedBy = null)
    {
        IsActive = true;
        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public RefreshToken AddRefreshToken(string token, DateTime expiresAtUtc, string? createdByIp = null)
    {
        var refreshToken = RefreshToken.Create(Id, token, expiresAtUtc, createdByIp);
        RefreshTokens.Add(refreshToken);
        return refreshToken;
    }
}
