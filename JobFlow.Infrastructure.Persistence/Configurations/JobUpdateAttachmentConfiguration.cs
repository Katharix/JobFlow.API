using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class JobUpdateAttachmentConfiguration : IEntityTypeConfiguration<JobUpdateAttachment>
{
    public void Configure(EntityTypeBuilder<JobUpdateAttachment> builder)
    {
        builder.ToTable("JobUpdateAttachments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FileSizeBytes).IsRequired();
        builder.Property(x => x.FileData).HasColumnType("varbinary(max)").IsRequired();
    }
}
