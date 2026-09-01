using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskForge.Domain.Common;
using TaskForge.Domain.Entities;

namespace TaskForge.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants", "public");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .HasMaxLength(Tenant.MaxNameLength)
            .IsRequired();

        builder.Property(t => t.SchemaName)
            .HasMaxLength(Tenant.MaxSchemaLength)
            .IsRequired();

        builder.Property(t => t.Slug)
            .HasConversion(slug => slug.Value, val => TenantSlug.Create(val))
            .HasMaxLength(TenantSlug.MaxLength)
            .IsRequired();

        builder.HasIndex(t => t.Slug)
            .IsUnique();

        builder.Property(t => t.Plan)
            .IsRequired();

        builder.Property(t => t.Status)
            .IsRequired();

        builder.HasMany(t => t.Memberships)
            .WithOne(m => m.Tenant)
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Invitations)
            .WithOne(i => i.Tenant)
            .HasForeignKey(i => i.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
