using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class FollowUpStepConfiguration : IEntityTypeConfiguration<FollowUpStep>
{
    public void Configure(EntityTypeBuilder<FollowUpStep> builder)
    {
        builder.ToTable("FollowUpSteps");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MessageTemplate).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => new { x.FollowUpSequenceId, x.StepOrder }).IsUnique();
    }
}
