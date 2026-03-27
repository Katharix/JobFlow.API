using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal sealed class DataExportJobConfiguration : IEntityTypeConfiguration<DataExportJob>
{
    public void Configure(EntityTypeBuilder<DataExportJob> builder)
    {
        builder.ToTable("DataExportJob");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(x => x.FileName)
            .HasMaxLength(255);

        builder.Property(x => x.ContentType)
            .HasMaxLength(128);

        builder.HasIndex(x => new { x.OrganizationId, x.CreatedAt });
        builder.HasIndex(x => new { x.OrganizationId, x.Status });

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}