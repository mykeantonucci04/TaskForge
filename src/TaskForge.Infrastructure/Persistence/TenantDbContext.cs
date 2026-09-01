using Microsoft.EntityFrameworkCore;
using TaskForge.Domain.Entities;
using TaskForge.Domain.Interfaces;

namespace TaskForge.Infrastructure.Persistence;

public class TenantDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public TenantDbContext(
        DbContextOptions<TenantDbContext> options,
        ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    // Tenant-Scoped Entities (Schema-per-Tenant)
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

        // Apply dynamic schema for tenant-scoped entities
        if (!string.IsNullOrWhiteSpace(_tenantContext.SchemaName))
        {
            modelBuilder.HasDefaultSchema(_tenantContext.SchemaName);
        }

        modelBuilder.Entity<Workspace>(builder =>
        {
            builder.ToTable("workspaces");
            builder.HasKey(w => w.Id);
            builder.HasIndex(w => w.Slug).IsUnique();
            builder.Property(w => w.Name).HasMaxLength(100).IsRequired();
            builder.Property(w => w.Slug).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Project>(builder =>
        {
            builder.ToTable("projects");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
            builder.Property(p => p.Key).HasMaxLength(10).IsRequired();
            builder.HasOne(p => p.Workspace).WithMany(w => w.Projects).HasForeignKey(p => p.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Board>(builder =>
        {
            builder.ToTable("boards");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Name).HasMaxLength(100).IsRequired();
            builder.HasOne(b => b.Project).WithMany(p => p.Boards).HasForeignKey(b => b.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BoardColumn>(builder =>
        {
            builder.ToTable("board_columns");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Name).HasMaxLength(100).IsRequired();
            builder.HasOne(c => c.Board).WithMany(b => b.Columns).HasForeignKey(c => c.BoardId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskItem>(builder =>
        {
            builder.ToTable("tasks");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
            builder.HasOne(t => t.Column).WithMany(c => c.Tasks).HasForeignKey(t => t.ColumnId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(t => t.Assignee).WithMany().HasForeignKey(t => t.AssigneeId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Subtask>(builder =>
        {
            builder.ToTable("subtasks");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Title).HasMaxLength(200).IsRequired();
            builder.HasOne(s => s.TaskItem).WithMany(t => t.Subtasks).HasForeignKey(s => s.TaskItemId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Comment>(builder =>
        {
            builder.ToTable("comments");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Content).IsRequired();
            builder.HasOne(c => c.TaskItem).WithMany(t => t.Comments).HasForeignKey(c => c.TaskItemId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(c => c.Author).WithMany().HasForeignKey(c => c.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Label>(builder =>
        {
            builder.ToTable("labels");
            builder.HasKey(l => l.Id);
            builder.Property(l => l.Name).HasMaxLength(50).IsRequired();
            builder.Property(l => l.ColorHex).HasMaxLength(10).IsRequired();
            builder.HasOne(l => l.Workspace).WithMany(w => w.Labels).HasForeignKey(l => l.WorkspaceId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Attachment>(builder =>
        {
            builder.ToTable("attachments");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.FileName).HasMaxLength(256).IsRequired();
            builder.Property(a => a.StoragePath).HasMaxLength(500).IsRequired();
            builder.Property(a => a.ContentType).HasMaxLength(100).IsRequired();
            builder.HasOne(a => a.TaskItem).WithMany(t => t.Attachments).HasForeignKey(a => a.TaskItemId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(a => a.UploadedBy).WithMany().HasForeignKey(a => a.UploadedById).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ActivityLog>(builder =>
        {
            builder.ToTable("activity_logs");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.EntityName).HasMaxLength(100).IsRequired();
            builder.Property(a => a.EntityId).HasMaxLength(100).IsRequired();
            builder.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(builder =>
        {
            builder.ToTable("notifications");
            builder.HasKey(n => n.Id);
            builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
            builder.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Webhook>(builder =>
        {
            builder.ToTable("webhooks");
            builder.HasKey(w => w.Id);
            builder.Property(w => w.Url).HasMaxLength(500).IsRequired();
            builder.Property(w => w.Secret).HasMaxLength(100).IsRequired();
            builder.Property(w => w.Events).HasMaxLength(256).IsRequired();
        });
    }
}
