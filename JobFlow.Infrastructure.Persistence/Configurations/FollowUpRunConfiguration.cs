using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class FollowUpRunConfiguration : IEntityTypeConfiguration<FollowUpRun>
{
    public void Configure(EntityTypeBuilder<FollowUpRun> builder)
    {
        builder.ToTable("FollowUpRuns");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.OrganizationId, x.SequenceType, x.TriggerEntityId, x.Status });
        builder.HasIndex(x => new { x.FollowUpSequenceId, x.OrganizationClientId, x.Status });

        builder.HasMany(x => x.ExecutionLogs)
            .WithOne(x => x.Run!)
            .HasForeignKey(x => x.FollowUpRunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
