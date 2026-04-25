using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class OrganizationOnboardingEventConfiguration : IEntityTypeConfiguration<OrganizationOnboardingEvent>
{
    public void Configure(EntityTypeBuilder<OrganizationOnboardingEvent> builder)
    {
        builder.ToTable("OrganizationOnboardingEvents");
        builder.Property(x => x.StepName)
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(x => x.EventType)
            .IsRequired()
            .HasMaxLength(100);
        builder.HasIndex(x => new { x.OrganizationId, x.StepName, x.EventType });

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
