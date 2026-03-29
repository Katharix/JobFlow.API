using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class FollowUpExecutionLogConfiguration : IEntityTypeConfiguration<FollowUpExecutionLog>
{
    public void Configure(EntityTypeBuilder<FollowUpExecutionLog> builder)
    {
        builder.ToTable("FollowUpExecutionLogs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.HasIndex(x => new { x.FollowUpRunId, x.StepOrder });
    }
}
