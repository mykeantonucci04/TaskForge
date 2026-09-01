using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskForge.Domain.Common;
using TaskForge.Domain.Entities;

namespace TaskForge.Infrastructure.Persistence.Configurations;

public class TenantInvitationConfiguration : IEntityTypeConfiguration<TenantInvitation>
{
    public void Configure(EntityTypeBuilder<TenantInvitation> builder)
    {
        builder.ToTable("tenant_invitations", "public");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Email)
            .HasConversion(email => email.Value, val => Email.Create(val))
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        builder.Property(i => i.Token)
            .HasMaxLength(128)
            .IsRequired();

        builder.HasIndex(i => i.Token)
            .IsUnique();

        builder.Property(i => i.Role)
            .IsRequired();

        builder.Property(i => i.Status)
            .IsRequired();

        builder.HasOne(i => i.Tenant)
            .WithMany(t => t.Invitations)
            .HasForeignKey(i => i.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.InvitedBy)
            .WithMany()
            .HasForeignKey(i => i.InvitedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
