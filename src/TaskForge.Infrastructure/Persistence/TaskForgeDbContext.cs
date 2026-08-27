using Microsoft.EntityFrameworkCore;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Interfaces;

namespace TaskForge.Infrastructure.Persistence;

public class TaskForgeDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public TaskForgeDbContext(
        DbContextOptions<TaskForgeDbContext> options,
        ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Board> Boards => Set<Board>();
    public DbSet<BoardColumn> BoardColumns => Set<BoardColumn>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Subtask> Subtasks => Set<Subtask>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Webhook> Webhooks => Set<Webhook>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply Schema per Tenant dynamically
        if (!string.IsNullOrWhiteSpace(_tenantContext.SchemaName))
        {
            modelBuilder.HasDefaultSchema(_tenantContext.SchemaName);
        }

        // Fluent API configurations
        modelBuilder.Entity<Tenant>(builder =>
        {
            builder.ToTable("tenants", "public"); // Tenants metadata table always in public schema
            builder.HasKey(t => t.Id);
            builder.HasIndex(t => t.Identifier).IsUnique();
            builder.Property(t => t.Name).HasMaxLength(100).IsRequired();
            builder.Property(t => t.SchemaName).HasMaxLength(63).IsRequired();
        });

        modelBuilder.Entity<User>(builder =>
        {
            builder.HasKey(u => u.Id);
            builder.HasIndex(u => u.Email).IsUnique();
            builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
            builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Workspace>(builder =>
        {
            builder.HasKey(w => w.Id);
            builder.HasIndex(w => w.Slug).IsUnique();
            builder.Property(w => w.Name).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Project>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
            builder.Property(p => p.Key).HasMaxLength(10).IsRequired();
            builder.HasOne(p => p.Workspace).WithMany(w => w.Projects).HasForeignKey(p => p.WorkspaceId);
        });

        modelBuilder.Entity<Board>(builder =>
        {
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Name).HasMaxLength(100).IsRequired();
            builder.HasOne(b => b.Project).WithMany(p => p.Boards).HasForeignKey(b => b.ProjectId);
        });

        modelBuilder.Entity<BoardColumn>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
            builder.HasOne(c => c.Board).WithMany(b => b.Columns).HasForeignKey(c => c.BoardId);
        });

        modelBuilder.Entity<TaskItem>(builder =>
        {
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
            builder.HasOne(t => t.Column).WithMany(c => c.Tasks).HasForeignKey(t => t.ColumnId);
            builder.HasOne(t => t.Assignee).WithMany().HasForeignKey(t => t.AssigneeId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Subtask>(builder =>
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Title).HasMaxLength(200).IsRequired();
            builder.HasOne(s => s.TaskItem).WithMany(t => t.Subtasks).HasForeignKey(s => s.TaskItemId);
        });

        modelBuilder.Entity<Comment>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Content).IsRequired();
            builder.HasOne(c => c.TaskItem).WithMany(t => t.Comments).HasForeignKey(c => c.TaskItemId);
            builder.HasOne(c => c.Author).WithMany().HasForeignKey(c => c.AuthorId);
        });

        modelBuilder.Entity<Label>(builder =>
        {
            builder.HasKey(l => l.Id);
            builder.Property(l => l.Name).HasMaxLength(50).IsRequired();
            builder.HasOne(l => l.Workspace).WithMany(w => w.Labels).HasForeignKey(l => l.WorkspaceId);
        });

        modelBuilder.Entity<Attachment>(builder =>
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.FileName).HasMaxLength(256).IsRequired();
            builder.HasOne(a => a.TaskItem).WithMany(t => t.Attachments).HasForeignKey(a => a.TaskItemId);
        });

        modelBuilder.Entity<ActivityLog>(builder =>
        {
            builder.HasKey(a => a.Id);
            builder.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId);
        });

        modelBuilder.Entity<Notification>(builder =>
        {
            builder.HasKey(n => n.Id);
            builder.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId);
        });

        modelBuilder.Entity<Webhook>(builder =>
        {
            builder.HasKey(w => w.Id);
            builder.Property(w => w.Url).HasMaxLength(500).IsRequired();
        });
    }
}
