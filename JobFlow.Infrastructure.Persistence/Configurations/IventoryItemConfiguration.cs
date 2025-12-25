using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Unit).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Category).HasMaxLength(100);
        builder.Property(x => x.CostPerUnit).HasPrecision(18, 2);

        builder.HasIndex(x => new { x.OrganizationId, x.Name }).IsUnique();
    }
}