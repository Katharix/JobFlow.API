using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class FollowUpSequenceConfiguration : IEntityTypeConfiguration<FollowUpSequence>
{
    public void Configure(EntityTypeBuilder<FollowUpSequence> builder)
    {
        builder.ToTable("FollowUpSequences");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.HasIndex(x => new { x.OrganizationId, x.SequenceType, x.IsActive });

        builder.HasMany(x => x.Steps)
            .WithOne(x => x.Sequence!)
            .HasForeignKey(x => x.FollowUpSequenceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
