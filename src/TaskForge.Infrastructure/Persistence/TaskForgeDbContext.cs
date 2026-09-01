using Microsoft.EntityFrameworkCore;
using TaskForge.Domain.Common;
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

    // Public Schema Entities
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<TenantInvitation> TenantInvitations => Set<TenantInvitation>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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

        // ==========================================
        // 1. PUBLIC SCHEMA CONFIGURATIONS
        // ==========================================

        modelBuilder.Entity<Tenant>(builder =>
        {
            builder.ToTable("tenants", "public");
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Name).HasMaxLength(Tenant.MaxNameLength).IsRequired();
            builder.Property(t => t.SchemaName).HasMaxLength(Tenant.MaxSchemaLength).IsRequired();

            builder.Property(t => t.Slug)
                .HasConversion(slug => slug.Value, val => TenantSlug.Create(val))
                .HasMaxLength(TenantSlug.MaxLength)
                .IsRequired();

            builder.HasIndex(t => t.Slug).IsUnique();

            builder.Property(t => t.Plan).IsRequired();
            builder.Property(t => t.Status).IsRequired();

            builder.HasMany(t => t.Memberships)
                .WithOne(m => m.Tenant)
                .HasForeignKey(m => m.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(t => t.Invitations)
                .WithOne(i => i.Tenant)
                .HasForeignKey(i => i.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users", "public");
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Email)
                .HasConversion(email => email.Value, val => Email.Create(val))
                .HasMaxLength(Email.MaxLength)
                .IsRequired();

            builder.HasIndex(u => u.Email).IsUnique();

            builder.Property(u => u.FirstName).HasMaxLength(User.MaxNameLength).IsRequired();
            builder.Property(u => u.LastName).HasMaxLength(User.MaxNameLength).IsRequired();
            builder.Property(u => u.PasswordHash).IsRequired();
            builder.Property(u => u.IsActive).IsRequired();

            builder.HasMany(u => u.Memberships)
                .WithOne(m => m.User)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.RefreshTokens)
                .WithOne(r => r.User)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TenantMembership>(builder =>
        {
            builder.ToTable("tenant_memberships", "public");
            builder.HasKey(m => m.Id);

            builder.HasIndex(m => new { m.TenantId, m.UserId }).IsUnique();

            builder.Property(m => m.Role).IsRequired();
            builder.Property(m => m.Status).IsRequired();

            builder.HasOne(m => m.Tenant)
                .WithMany(t => t.Memberships)
                .HasForeignKey(m => m.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(m => m.User)
                .WithMany(u => u.Memberships)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TenantInvitation>(builder =>
        {
            builder.ToTable("tenant_invitations", "public");
            builder.HasKey(i => i.Id);

            builder.Property(i => i.Email)
                .HasConversion(email => email.Value, val => Email.Create(val))
                .HasMaxLength(Email.MaxLength)
                .IsRequired();

            builder.Property(i => i.Token).HasMaxLength(128).IsRequired();
            builder.HasIndex(i => i.Token).IsUnique();

            builder.Property(i => i.Role).IsRequired();
            builder.Property(i => i.Status).IsRequired();

            builder.HasOne(i => i.Tenant)
                .WithMany(t => t.Invitations)
                .HasForeignKey(i => i.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(i => i.InvitedBy)
                .WithMany()
                .HasForeignKey(i => i.InvitedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(builder =>
        {
            builder.ToTable("refresh_tokens", "public");
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Token).HasMaxLength(256).IsRequired();
            builder.HasIndex(r => r.Token).IsUnique();
            builder.Property(r => r.CreatedByIp).HasMaxLength(50);
            builder.Property(r => r.RevokedByIp).HasMaxLength(50);
            builder.Property(r => r.ReplacedByToken).HasMaxLength(256);

            builder.HasOne(r => r.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================
        // 2. TENANT-SCOPED SCHEMA CONFIGURATIONS
        // ==========================================

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
