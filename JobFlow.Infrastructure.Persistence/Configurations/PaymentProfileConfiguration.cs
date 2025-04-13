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
    internal class PaymentProfileConfiguration : IEntityTypeConfiguration<CustomerPaymentProfile>
    {
        public void Configure(EntityTypeBuilder<CustomerPaymentProfile> builder)
        {
            builder.ToTable("CustomerPaymentProfile", "payment");
            builder.HasKey(e => e.Id);
        }
    }
}
