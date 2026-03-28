using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class OrganizationInvoicingSettingsConfiguration : IEntityTypeConfiguration<OrganizationInvoicingSettings>
{
    public void Configure(EntityTypeBuilder<OrganizationInvoicingSettings> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.OrganizationId).IsUnique();

        builder.Property(x => x.DefaultWorkflow)
            .HasConversion<int>()
            .HasDefaultValue(JobFlow.Domain.Enums.InvoicingWorkflow.SendInvoice);

        builder.Property(x => x.DepositPercentage)
            .HasPrecision(5, 2);
    }
}