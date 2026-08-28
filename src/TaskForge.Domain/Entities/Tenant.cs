using TaskForge.Domain.Common;
using TaskForge.Domain.Enums;
using TaskForge.Domain.Events;
using TaskForge.Shared.Exceptions;

namespace TaskForge.Domain.Entities;

public class Tenant : AggregateRoot<Guid>, IAuditableEntity
{
    public const int MaxNameLength = 100;
    public const int MaxSchemaLength = 63; // PostgreSQL max identifier length

    public string Name { get; private set; } = string.Empty;
    public TenantSlug Slug { get; private set; } = null!;
    public TenantPlan Plan { get; private set; } = TenantPlan.Free;
    public TenantStatus Status { get; private set; } = TenantStatus.Active;
    public string SchemaName { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    public ICollection<TenantMembership> Memberships { get; private set; } = new List<TenantMembership>();
    public ICollection<TenantInvitation> Invitations { get; private set; } = new List<TenantInvitation>();

    // Private constructor for EF Core
    private Tenant() { }

    private Tenant(Guid id, string name, TenantSlug slug, string schemaName, TenantPlan plan, string? createdBy)
        : base(id)
    {
        Name = name;
        Slug = slug;
        SchemaName = schemaName;
        Plan = plan;
        Status = TenantStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    public static Tenant Create(string name, TenantSlug slug, TenantPlan plan = TenantPlan.Free, string? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Tenant name cannot be empty.");
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > MaxNameLength)
        {
            throw new DomainException($"Tenant name cannot exceed {MaxNameLength} characters.");
        }

        ArgumentNullException.ThrowIfNull(slug);

        var sanitizedSlug = slug.Value.Replace('-', '_');
        var schemaName = $"tenant_{sanitizedSlug}";
        if (schemaName.Length > MaxSchemaLength)
        {
            schemaName = schemaName[..MaxSchemaLength];
        }

        var tenantId = Guid.NewGuid();
        var tenant = new Tenant(tenantId, trimmedName, slug, schemaName, plan, createdBy);

        tenant.AddDomainEvent(new TenantCreatedEvent(
            tenant.Id,
            tenant.Name,
            tenant.Slug.Value,
            tenant.SchemaName,
            tenant.Plan));

        return tenant;
    }

    public void UpdateDetails(string name, string? modifiedBy = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Tenant name cannot be empty.");
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > MaxNameLength)
        {
            throw new DomainException($"Tenant name cannot exceed {MaxNameLength} characters.");
        }

        Name = trimmedName;
        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void ChangePlan(TenantPlan newPlan, string? modifiedBy = null)
    {
        Plan = newPlan;
        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void Suspend(string? modifiedBy = null)
    {
        if (Status == TenantStatus.Suspended) return;
        Status = TenantStatus.Suspended;
        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void Activate(string? modifiedBy = null)
    {
        if (Status == TenantStatus.Active) return;
        Status = TenantStatus.Active;
        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }

    public void Cancel(string? modifiedBy = null)
    {
        if (Status == TenantStatus.Cancelled) return;
        Status = TenantStatus.Cancelled;
        LastModifiedAtUtc = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }
}
