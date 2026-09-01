using Microsoft.EntityFrameworkCore;
using TaskForge.Domain.Interfaces;

namespace TaskForge.Infrastructure.Persistence;

[Obsolete("TaskForgeDbContext has been split into PublicDbContext and TenantDbContext. Use PublicDbContext or TenantDbContext instead.")]
public class TaskForgeDbContext : PublicDbContext
{
    public TaskForgeDbContext(DbContextOptions<PublicDbContext> options)
        : base(options)
    {
    }
}
