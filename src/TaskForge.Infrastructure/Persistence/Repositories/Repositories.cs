using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TaskForge.Domain.Common;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Interfaces;

namespace TaskForge.Infrastructure.Persistence.Repositories;

public class Repository<TEntity>(TaskForgeDbContext context) : IRepository<TEntity>
    where TEntity : class
{
    protected TaskForgeDbContext Context { get; } = context;
    protected DbSet<TEntity> DbSet { get; } = context.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var entry = await DbSet.AddAsync(entity, cancellationToken);
        return entry.Entity;
    }

    public void Update(TEntity entity)
    {
        DbSet.Update(entity);
    }

    public void Remove(TEntity entity)
    {
        DbSet.Remove(entity);
    }
}

public class TenantRepository(TaskForgeDbContext context) : Repository<Tenant>(context), ITenantRepository
{
    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(t => t.Memberships)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Tenant?> GetBySlugAsync(TenantSlug slug, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(t => t.Memberships)
            .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
    }

    public async Task<Tenant?> GetByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var slug = TenantSlug.Create(identifier);
        return await DbSet
            .Include(t => t.Memberships)
            .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
    }

    public async Task<bool> ExistsBySlugAsync(TenantSlug slug, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(t => t.Slug == slug, cancellationToken);
    }
}

public class UserRepository(TaskForgeDbContext context) : Repository<User>(context), IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(u => u.Memberships)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(u => u.Memberships)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(u => u.Email == email, cancellationToken);
    }
}

public class TenantMembershipRepository(TaskForgeDbContext context) : Repository<TenantMembership>(context), ITenantMembershipRepository
{
    public async Task<TenantMembership?> GetByTenantAndUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(m => m.Tenant)
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<TenantMembership>> GetMembershipsByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(m => m.Tenant)
            .Where(m => m.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TenantMembership>> GetMembershipsByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(m => m.User)
            .Where(m => m.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }
}

public class TaskRepository(TaskForgeDbContext context) : Repository<TaskItem>(context), ITaskRepository
{
    public async Task<TaskItem?> GetTaskWithDetailsAsync(Guid taskId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(t => t.Column)
            .Include(t => t.Assignee)
            .Include(t => t.Subtasks)
            .Include(t => t.Comments)
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
    }

    public async Task<IReadOnlyList<TaskItem>> GetTasksByColumnAsync(Guid columnId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(t => t.ColumnId == columnId)
            .OrderBy(t => t.Position)
            .ToListAsync(cancellationToken);
    }
}

public class WorkspaceRepository(TaskForgeDbContext context) : Repository<Workspace>(context), IWorkspaceRepository
{
    public async Task<Workspace?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(w => w.Slug == slug, cancellationToken);
    }
}

public class UnitOfWork(TaskForgeDbContext context) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }
}
