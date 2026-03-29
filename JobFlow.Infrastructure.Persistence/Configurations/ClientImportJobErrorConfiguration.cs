using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal sealed class ClientImportJobErrorConfiguration : IEntityTypeConfiguration<ClientImportJobError>
{
    public void Configure(EntityTypeBuilder<ClientImportJobError> builder)
    {
        builder.ToTable("ClientImportJobError");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Message)
            .HasMaxLength(2000)
            .IsRequired();

        builder.HasIndex(x => x.ClientImportJobId);
        builder.HasIndex(x => x.RowNumber);

        builder.HasOne(x => x.ClientImportJob)
            .WithMany(x => x.Errors)
            .HasForeignKey(x => x.ClientImportJobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
