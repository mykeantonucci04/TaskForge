using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskForge.Domain.Entities;

namespace TaskForge.Infrastructure.Persistence.Configurations;

public class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("tenant_memberships", "public");

        builder.HasKey(m => m.Id);

        builder.HasIndex(m => new { m.TenantId, m.UserId })
            .IsUnique();

        builder.Property(m => m.Role)
            .IsRequired();

        builder.Property(m => m.Status)
            .IsRequired();

        builder.HasOne(m => m.Tenant)
            .WithMany(t => t.Memberships)
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
            .WithMany(u => u.Memberships)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
