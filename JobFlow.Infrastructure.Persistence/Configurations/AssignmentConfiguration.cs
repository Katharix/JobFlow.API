using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignment");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ScheduleType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.ScheduledStart)
            .IsRequired();

        builder.Property(x => x.ScheduledEnd)
            .IsRequired(false);

        builder.HasOne(x => x.Job)
            .WithMany(j => j.Assignments)
            .HasForeignKey(x => x.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.JobId);
        builder.HasIndex(x => x.ScheduledStart);
    }
}