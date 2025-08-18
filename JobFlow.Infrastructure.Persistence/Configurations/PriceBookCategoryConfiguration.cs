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
    public class PriceBookCategoryConfiguration : IEntityTypeConfiguration<PriceBookCategory>
    {
        public void Configure(EntityTypeBuilder<PriceBookCategory> builder)
        {
            builder.ToTable("PriceBookCategories");

            builder.HasKey(x => x.Id);
            builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Description)
                .HasMaxLength(500);

            builder.HasIndex(x => new { x.OrganizationId, x.Name })
                .IsUnique();
        }
    }

}
