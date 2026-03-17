using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal class SecurityAlertConfiguration : IEntityTypeConfiguration<SecurityAlert>
{
    public void Configure(EntityTypeBuilder<SecurityAlert> builder)
    {
        builder.ToTable("SecurityAlert", "security");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.RuleKey).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Category).HasMaxLength(80).IsRequired();
        builder.Property(e => e.Severity).HasMaxLength(20).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(1024).IsRequired();
        builder.Property(e => e.Status).HasMaxLength(20).IsRequired();

        builder.HasIndex(e => new { e.RuleKey, e.Status, e.CreatedAt });
        builder.HasIndex(e => e.CreatedAt);
    }
}
