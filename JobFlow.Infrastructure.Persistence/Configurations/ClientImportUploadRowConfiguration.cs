using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal sealed class ClientImportUploadRowConfiguration : IEntityTypeConfiguration<ClientImportUploadRow>
{
    public void Configure(EntityTypeBuilder<ClientImportUploadRow> builder)
    {
        builder.ToTable("ClientImportUploadRow");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RowDataJson)
            .IsRequired();

        builder.HasIndex(x => new { x.ClientImportUploadSessionId, x.RowNumber })
            .IsUnique();

        builder.HasOne(x => x.Session)
            .WithMany(x => x.Rows)
            .HasForeignKey(x => x.ClientImportUploadSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
