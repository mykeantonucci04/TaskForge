using TaskForge.Domain.Interfaces;
using TaskForge.Shared.Constants;

namespace TaskForge.Infrastructure.Persistence;

public class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }
    public string SchemaName { get; private set; } = TenantConstants.DefaultSchema;
    public bool IsResolved => TenantId.HasValue;

    public void SetTenant(Guid tenantId, string schemaName)
    {
        TenantId = tenantId;
        SchemaName = string.IsNullOrWhiteSpace(schemaName) ? TenantConstants.DefaultSchema : schemaName;
    }
}
