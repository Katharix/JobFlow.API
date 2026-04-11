using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLog", "security");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Category).HasMaxLength(80).IsRequired();
        builder.Property(e => e.Action).HasMaxLength(20).IsRequired();
        builder.Property(e => e.ResourceType).HasMaxLength(120).IsRequired();
        builder.Property(e => e.ResourceId).HasMaxLength(80);
        builder.Property(e => e.Path).HasMaxLength(512);
        builder.Property(e => e.Method).HasMaxLength(10);
        builder.Property(e => e.UserId).HasMaxLength(128);
        builder.Property(e => e.IpAddress).HasMaxLength(64);
        builder.Property(e => e.UserAgent).HasMaxLength(512);
        builder.Property(e => e.DetailsJson);

        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => new { e.OrganizationId, e.CreatedAt });
        builder.HasIndex(e => new { e.UserId, e.CreatedAt });
    }
}
