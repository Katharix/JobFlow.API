using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Infrastructure.Persistence.Configurations
{
    public class PriceBookItemConfiguration : IEntityTypeConfiguration<PriceBookItem>
    {
        public void Configure(EntityTypeBuilder<PriceBookItem> builder)
        {
            builder.ToTable("PriceBookItems");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Unit).IsRequired().HasMaxLength(50);
            builder.Property(x => x.Description).HasMaxLength(1000);
            builder.Property(x => x.Category).HasMaxLength(100);
            builder.Property(x => x.PricePerUnit).HasColumnType("decimal(18,2)");

            builder
                .HasOne(x => x.InventoryItem)
                .WithMany()
                .HasForeignKey(x => x.InventoryItemId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(x => new { x.OrganizationId, x.Name }).IsUnique();
        }
    }

}
