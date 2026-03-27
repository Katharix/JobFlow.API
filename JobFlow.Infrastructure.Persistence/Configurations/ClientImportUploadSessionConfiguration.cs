using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal sealed class ClientImportUploadSessionConfiguration : IEntityTypeConfiguration<ClientImportUploadSession>
{
    public void Configure(EntityTypeBuilder<ClientImportUploadSession> builder)
    {
        builder.ToTable("ClientImportUploadSession");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceSystem)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(x => new { x.OrganizationId, x.Status, x.ExpiresAtUtc });

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
