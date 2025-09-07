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
    internal class InvoiceSequenceConfiguration : IEntityTypeConfiguration<InvoiceSequence>
    {
        public void Configure(EntityTypeBuilder<InvoiceSequence> builder)
        {
            builder.ToTable("InvoiceSequence");
            builder.HasKey(e => e.Id);
        }
    }
}
