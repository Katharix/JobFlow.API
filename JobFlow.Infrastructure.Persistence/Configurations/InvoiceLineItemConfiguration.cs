using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal class InvoiceLineItemConfiguration : EntityTypeConfiguration<InvoiceLineItem>
{
    public override void Map(EntityTypeBuilder<InvoiceLineItem> builder)
    {
        builder.ToTable("InvoiceLineItem");
        builder.HasKey(e => e.Id);
        builder.Property(x => x.UnitPrice)
            .HasPrecision(18, 2);
    }
}