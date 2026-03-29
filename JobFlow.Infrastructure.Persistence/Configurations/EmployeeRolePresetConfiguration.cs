using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal class EmployeeRolePresetConfiguration : IEntityTypeConfiguration<EmployeeRolePreset>
{
    public void Configure(EntityTypeBuilder<EmployeeRolePreset> builder)
    {
        builder.ToTable("EmployeeRolePresets");
        builder.HasKey(preset => preset.Id);
        builder.Property(preset => preset.Name)
            .IsRequired()
            .HasMaxLength(120);
        builder.Property(preset => preset.Description)
            .HasMaxLength(240);
        builder.Property(preset => preset.IndustryKey)
            .HasMaxLength(80);
        builder.HasOne(preset => preset.Organization)
            .WithMany()
            .HasForeignKey(preset => preset.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(preset => preset.IsActive);

        var createdAt = new DateTime(2026, 3, 23, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new EmployeeRolePreset
            {
                Id = Guid.Parse("1a2b3c4d-1111-1111-1111-111111111111"),
                Name = "Home services",
                Description = "Default roles for field service teams.",
                IndustryKey = "home-services",
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },
            new EmployeeRolePreset
            {
                Id = Guid.Parse("1a2b3c4d-2222-2222-2222-222222222222"),
                Name = "Creative",
                Description = "Default roles for creative studios.",
                IndustryKey = "creative",
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },
            new EmployeeRolePreset
            {
                Id = Guid.Parse("1a2b3c4d-3333-3333-3333-333333333333"),
                Name = "Consulting",
                Description = "Default roles for consulting teams.",
                IndustryKey = "consulting",
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            },
            new EmployeeRolePreset
            {
                Id = Guid.Parse("1a2b3c4d-4444-4444-4444-444444444444"),
                Name = "Tech repair",
                Description = "Default roles for repair shops.",
                IndustryKey = "tech-repair",
                IsSystem = true,
                CreatedAt = createdAt,
                IsActive = true
            }
        );
    }
}
