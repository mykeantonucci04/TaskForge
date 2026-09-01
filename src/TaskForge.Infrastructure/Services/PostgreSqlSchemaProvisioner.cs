using TaskForge.Application.Common.Interfaces;

namespace TaskForge.Infrastructure.Services;

public class PostgreSqlSchemaProvisioner(ITenantProvisioningService provisioningService) : ISchemaProvisioner
{
    public Task ProvisionTenantSchemaAsync(string schemaName, CancellationToken cancellationToken = default)
    {
        return provisioningService.ProvisionTenantSchemaAsync(schemaName, cancellationToken);
    }
}
