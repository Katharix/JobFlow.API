using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class EstimateConfiguration : IEntityTypeConfiguration<Estimate>
{
    public void Configure(EntityTypeBuilder<Estimate> builder)
    {
        builder.ToTable("Estimates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EstimateNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => new { x.OrganizationId, x.EstimateNumber }).IsUnique();

        builder.Property(x => x.PublicToken).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => x.PublicToken).IsUnique();

        builder.Property(x => x.Subtotal).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TaxTotal).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Total).HasColumnType("decimal(18,2)");

        builder.HasMany(x => x.LineItems)
            .WithOne(x => x.Estimate!)
            .HasForeignKey(x => x.EstimateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.RevisionRequests)
            .WithOne(x => x.Estimate)
            .HasForeignKey(x => x.EstimateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}