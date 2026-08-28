namespace TaskForge.Application.Common.Interfaces;

public interface ISchemaProvisioner
{
    Task ProvisionTenantSchemaAsync(string schemaName, CancellationToken cancellationToken = default);
}
