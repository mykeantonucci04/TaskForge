using TaskForge.Domain.Common;
using TaskForge.Domain.Enums;
using TaskForge.Shared.Exceptions;

namespace TaskForge.Domain.Entities;

public class TenantMembership : Entity<Guid>, IAuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public MembershipRole Role { get; private set; } = MembershipRole.Member;
    public MembershipStatus Status { get; private set; } = MembershipStatus.Active;
    public DateTime? JoinedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    public Tenant Tenant { get; private set; } = null!;
    public User User { get; private set; } = null!;

    // Private constructor for EF Core
    private TenantMembership() { }

    private TenantMembership(Guid id, Guid tenantId, Guid userId, MembershipRole role, MembershipStatus status, string? createdBy)
        : base(id)
    {
        TenantId = tenantId;
        UserId = userId;
        Role = role;
        Status = status;
        JoinedAtUtc = status == MembershipStatus.Active ? DateTime.UtcNow : null;
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    public static TenantMembership Create(
        Guid tenantId,
        Guid userId,
        MembershipRole role,
        MembershipStatus status = MembershipStatus.Active,
        string? createdBy = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("Tenant ID cannot be empty.");
        }

        if (userId == Guid.Empty)
        {
            throw new DomainException("User ID cannot be empty.");
        }

        var membershipId = Guid.NewGuid();
        return new TenantMembership(membershipId, tenantId, userId, role, status, createdBy);
    }

    public void ChangeRole(MembershipRole newRole, string? modifiedBy = null)
    {
        Role = newRole;
        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void Activate(string? modifiedBy = null)
    {
        Status = MembershipStatus.Active;
        JoinedAtUtc ??= DateTime.UtcNow;
        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void Remove(string? modifiedBy = null)
    {
        Status = MembershipStatus.Removed;
        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }
}
