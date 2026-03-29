using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal sealed class ClientImportJobConfiguration : IEntityTypeConfiguration<ClientImportJob>
{
    public void Configure(EntityTypeBuilder<ClientImportJob> builder)
    {
        builder.ToTable("ClientImportJob");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceSystem)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(2000);

        builder.HasIndex(x => new { x.OrganizationId, x.CreatedAt });
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
