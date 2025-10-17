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
    internal class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Order", "payment");
            builder.HasKey(e => e.Id);
            builder.Property(x => x.TotalAmount)
                   .HasPrecision(18, 2);

        }
    }
}
