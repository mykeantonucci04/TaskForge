using System.Security.Cryptography;
using TaskForge.Domain.Common;
using TaskForge.Domain.Enums;
using TaskForge.Shared.Exceptions;

namespace TaskForge.Domain.Entities;

public class TenantInvitation : Entity<Guid>, IAuditableEntity
{
    public Guid TenantId { get; private set; }
    public Email Email { get; private set; } = null!;
    public MembershipRole Role { get; private set; } = MembershipRole.Member;
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public MembershipStatus Status { get; private set; } = MembershipStatus.Pending;
    public Guid InvitedById { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    public Tenant Tenant { get; private set; } = null!;
    public User InvitedBy { get; private set; } = null!;

    // Private constructor for EF Core
    private TenantInvitation() { }

    private TenantInvitation(
        Guid id,
        Guid tenantId,
        Email email,
        MembershipRole role,
        string token,
        DateTime expiresAtUtc,
        Guid invitedById,
        string? createdBy)
        : base(id)
    {
        TenantId = tenantId;
        Email = email;
        Role = role;
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
        Status = MembershipStatus.Pending;
        InvitedById = invitedById;
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    public static TenantInvitation Create(
        Guid tenantId,
        Email email,
        MembershipRole role,
        Guid invitedById,
        int expirationDays = 7,
        string? createdBy = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("Tenant ID cannot be empty.");
        }

        ArgumentNullException.ThrowIfNull(email);

        if (invitedById == Guid.Empty)
        {
            throw new DomainException("InvitedBy User ID cannot be empty.");
        }

        if (expirationDays <= 0)
        {
            throw new DomainException("Expiration days must be greater than zero.");
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiresAtUtc = DateTime.UtcNow.AddDays(expirationDays);
        var invitationId = Guid.NewGuid();

        return new TenantInvitation(invitationId, tenantId, email, role, token, expiresAtUtc, invitedById, createdBy);
    }

    public bool IsExpired(DateTime now) => now >= ExpiresAtUtc || Status != MembershipStatus.Pending;

    public void Accept(string? modifiedBy = null)
    {
        if (Status != MembershipStatus.Pending)
        {
            throw new DomainException($"Invitation cannot be accepted in state '{Status}'.");
        }

        if (DateTime.UtcNow >= ExpiresAtUtc)
        {
            throw new DomainException("Invitation has expired.");
        }

        Status = MembershipStatus.Active;
        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void Revoke(string? modifiedBy = null)
    {
        Status = MembershipStatus.Removed;
        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }
}
