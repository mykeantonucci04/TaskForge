using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace TaskForge.Infrastructure.Persistence;

/// <summary>
/// Custom EF Core ModelCacheKeyFactory for schema-per-tenant isolation.
/// Ensures EF Core compiles and caches distinct IModel instances per tenant schema,
/// preventing cross-tenant model leakage and ensuring queries execute against the correct schema.
/// </summary>
public class TenantModelCacheKeyFactory : ModelCacheKeyFactory
{
    public TenantModelCacheKeyFactory(ModelCacheKeyFactoryDependencies dependencies)
        : base(dependencies)
    {
    }

    public override object Create(DbContext context, bool designTime)
    {
        if (context is TenantDbContext tenantDbContext)
        {
            return (context.GetType(), tenantDbContext.SchemaName, designTime);
        }

        return base.Create(context, designTime);
    }
}
