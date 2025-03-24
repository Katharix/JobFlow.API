using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Infrastructure.Persistence.Configurations
{
    internal class PricingTierConfiguration : IEntityTypeConfiguration<PricingTier>
    {
        public void Configure(EntityTypeBuilder<PricingTier> builder)
        {
            builder.ToTable("PricingTier", "site");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id).HasDefaultValueSql("NEWID()");
        }
    }
}
