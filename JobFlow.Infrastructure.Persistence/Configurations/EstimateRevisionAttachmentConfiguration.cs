using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class EstimateRevisionAttachmentConfiguration : IEntityTypeConfiguration<EstimateRevisionAttachment>
{
    public void Configure(EntityTypeBuilder<EstimateRevisionAttachment> builder)
    {
        builder.ToTable("EstimateRevisionAttachments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FileSizeBytes).IsRequired();
        builder.Property(x => x.FileData).HasColumnType("varbinary(max)").IsRequired();
    }
}
