using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class EstimateRevisionRequestConfiguration : IEntityTypeConfiguration<EstimateRevisionRequest>
{
    public void Configure(EntityTypeBuilder<EstimateRevisionRequest> builder)
    {
        builder.ToTable("EstimateRevisionRequests");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RevisionNumber).IsRequired();
        builder.Property(x => x.RequestMessage).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.OrganizationResponseMessage).HasMaxLength(4000);

        builder.HasIndex(x => new { x.EstimateId, x.RevisionNumber }).IsUnique();

        builder.HasOne(x => x.OrganizationClient)
            .WithMany()
            .HasForeignKey(x => x.OrganizationClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Attachments)
            .WithOne(x => x.RevisionRequest)
            .HasForeignKey(x => x.EstimateRevisionRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
