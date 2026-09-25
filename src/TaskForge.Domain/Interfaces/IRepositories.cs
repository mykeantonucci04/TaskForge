using System.Linq.Expressions;
using TaskForge.Domain.Common;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Enums;

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

public interface ITenantRepository : IRepository<Tenant>
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Tenant?> GetBySlugAsync(TenantSlug slug, CancellationToken cancellationToken = default);
    Task<Tenant?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default);
    Task<bool> ExistsBySlugAsync(TenantSlug slug, CancellationToken cancellationToken = default);
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default);
}

public interface ITenantMembershipRepository : IRepository<TenantMembership>
{
    Task<TenantMembership?> GetByTenantAndUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantMembership>> GetMembershipsByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantMembership>> GetMembershipsByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface ITaskRepository : IRepository<TaskItem>
{
    Task<TaskItem?> GetTaskWithDetailsAsync(Guid taskId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> GetTasksByColumnAsync(Guid columnId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TaskItem>> SearchTasksAsync(Guid? columnId, Guid? assigneeId, TaskPriority? priority, TaskItemStatus? status, Guid? labelId, CancellationToken cancellationToken = default);
}

public interface ISubtaskRepository : IRepository<Subtask>
{
    Task<IReadOnlyList<Subtask>> GetSubtasksByTaskAsync(Guid taskItemId, CancellationToken cancellationToken = default);
}

public interface ICommentRepository : IRepository<Comment>
{
    Task<IReadOnlyList<Comment>> GetCommentsByTaskAsync(Guid taskItemId, CancellationToken cancellationToken = default);
}

public interface ILabelRepository : IRepository<Label>
{
    Task<IReadOnlyList<Label>> GetLabelsByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(Guid workspaceId, string name, CancellationToken cancellationToken = default);
}

public interface IWorkspaceRepository : IRepository<Workspace>
{
    Task<Workspace?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}

public interface IProjectRepository : IRepository<Project>
{
    Task<Project?> GetByIdWithBoardsAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> GetProjectsByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByKeyAsync(Guid workspaceId, string key, CancellationToken cancellationToken = default);
}

public interface IBoardRepository : IRepository<Board>
{
    Task<Board?> GetBoardWithColumnsAndTasksAsync(Guid boardId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Board>> GetBoardsByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
}

public interface IBoardColumnRepository : IRepository<BoardColumn>
{
    Task<IReadOnlyList<BoardColumn>> GetColumnsByBoardAsync(Guid boardId, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
