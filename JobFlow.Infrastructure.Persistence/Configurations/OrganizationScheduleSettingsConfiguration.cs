using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

public class OrganizationScheduleSettingsConfiguration : IEntityTypeConfiguration<OrganizationScheduleSettings>
{
    public void Configure(EntityTypeBuilder<OrganizationScheduleSettings> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.OrganizationId).IsUnique();

        builder.Property(x => x.TravelBufferMinutes).HasDefaultValue(20);
        builder.Property(x => x.DefaultWindowMinutes).HasDefaultValue(120);
        builder.Property(x => x.EnforceTravelBuffer).HasDefaultValue(true);
        builder.Property(x => x.AutoNotifyReschedule).HasDefaultValue(true);
    }
}
