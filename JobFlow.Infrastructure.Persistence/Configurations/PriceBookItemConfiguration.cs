using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations
{
    public class PriceBookItemConfiguration : IEntityTypeConfiguration<PriceBookItem>
    {
        public void Configure(EntityTypeBuilder<PriceBookItem> builder)
        {
            builder.ToTable("PriceBookItems");

            builder.HasKey(x => x.Id);
            builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Unit)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Description)
                .HasMaxLength(1000);

            builder.Property(x => x.PricePerUnit)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.Type)
                .IsRequired();

            builder
                .HasOne(x => x.InventoryItem)
                .WithMany()
                .HasForeignKey(x => x.InventoryItemId)
                .OnDelete(DeleteBehavior.SetNull);

            builder
                .HasOne(x => x.Category)
                .WithMany(c => c.Items)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Property(x => x.InventoryUnitsPerSale)
                .HasColumnType("decimal(18,4)")
                .HasDefaultValue(1.0m);

            builder.HasIndex(x => new { x.OrganizationId, x.Name }).IsUnique();
        }
    }
}
