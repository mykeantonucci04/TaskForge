using System.Linq.Expressions;
using TaskForge.Domain.Entities;

namespace TaskForge.Domain.Interfaces;

public interface ITenantContext
{
    Guid? TenantId { get; }
    string SchemaName { get; }
    bool IsResolved { get; }
    void SetTenant(Guid tenantId, string schemaName);
}

public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}

public interface ITaskRepository : IRepository<TaskItem>
{
    Task<TaskItem?> GetTaskWithDetailsAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetTasksByColumnAsync(Guid columnId, CancellationToken cancellationToken = default);
}

public interface IWorkspaceRepository : IRepository<Workspace>
{
    Task<Workspace?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
