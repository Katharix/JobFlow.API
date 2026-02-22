using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class PriceBookCategoryConfiguration : IEntityTypeConfiguration<PriceBookCategory>
{
    public void Configure(EntityTypeBuilder<PriceBookCategory> builder)
    {
        builder.ToTable("PriceBookCategories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.OrganizationId, x.Name })
            .IsUnique();
    }
}