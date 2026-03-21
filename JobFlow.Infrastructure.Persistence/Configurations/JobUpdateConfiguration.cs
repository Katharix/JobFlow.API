using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class JobUpdateConfiguration : IEntityTypeConfiguration<JobUpdate>
{
    public void Configure(EntityTypeBuilder<JobUpdate> builder)
    {
        builder.ToTable("JobUpdates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Message).HasMaxLength(4000);
        builder.Property(x => x.Type).IsRequired();
        builder.Property(x => x.OccurredAt).IsRequired();

        builder.HasIndex(x => new { x.JobId, x.OccurredAt });

        builder.HasOne(x => x.Job)
            .WithMany(j => j.JobUpdates)
            .HasForeignKey(x => x.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Attachments)
            .WithOne(x => x.JobUpdate)
            .HasForeignKey(x => x.JobUpdateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
