using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JobFlow.Infrastructure.Persistence.Configurations;

internal class EmployeeRolePresetItemConfiguration : IEntityTypeConfiguration<EmployeeRolePresetItem>
{
    public void Configure(EntityTypeBuilder<EmployeeRolePresetItem> builder)
    {
        builder.ToTable("EmployeeRolePresetItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Name)
            .IsRequired()
            .HasMaxLength(120);
        builder.Property(item => item.Description)
            .HasMaxLength(240);
        builder.HasOne(item => item.Preset)
            .WithMany(preset => preset.Items)
            .HasForeignKey(item => item.PresetId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(item => item.IsActive);

        var createdAt = new DateTime(2026, 3, 23, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new EmployeeRolePresetItem
            {
                Id = Guid.Parse("2a2b3c4d-1111-1111-1111-111111111111"),
                PresetId = Guid.Parse("1a2b3c4d-1111-1111-1111-111111111111"),
                Name = "Technician",
                Description = "Field technician for on-site work.",
                SortOrder = 1,
                CreatedAt = createdAt,
                IsActive = true
            },
            new EmployeeRolePresetItem
            {
                Id = Guid.Parse("2a2b3c4d-1111-1111-1111-111111111112"),
                PresetId = Guid.Parse("1a2b3c4d-1111-1111-1111-111111111111"),
                Name = "Supervisor",
                Description = "Lead for quality checks and approvals.",
                SortOrder = 2,
                CreatedAt = createdAt,
                IsActive = true
            },
            new EmployeeRolePresetItem
            {
                Id = Guid.Parse("2a2b3c4d-1111-1111-1111-111111111113"),
                PresetId = Guid.Parse("1a2b3c4d-1111-1111-1111-111111111111"),
                Name = "Dispatcher",
                Description = "Routes schedules and job assignments.",
                SortOrder = 3,
                CreatedAt = createdAt,
                IsActive = true
            },
            new EmployeeRolePresetItem
            {
                Id = Guid.Parse("2a2b3c4d-1111-1111-1111-111111111114"),
                PresetId = Guid.Parse("1a2b3c4d-1111-1111-1111-111111111111"),
                Name = "Admin",
                Description = "Back-office support and billing.",
                SortOrder = 4,
                CreatedAt = createdAt,
                IsActive = true
            },
            new EmployeeRolePresetItem
            {
                Id = Guid.Parse("2a2b3c4d-2222-2222-2222-222222222221"),
                PresetId = Guid.Parse("1a2b3c4d-2222-2222-2222-222222222222"),
                Name = "Designer",
                Description = "Primary creator and deliverable owner.",
                SortOrder = 1,
                CreatedAt = createdAt,
                IsActive = true
            },
            new EmployeeRolePresetItem
            {
                Id = Guid.Parse("2a2b3c4d-2222-2222-2222-222222222222"),
                PresetId = Guid.Parse("1a2b3c4d-2222-2222-2222-222222222222"),
                Name = "Producer",
                Description = "Owns timelines, approvals, and client comms.",
                SortOrder = 2,
                CreatedAt = createdAt,
                IsActive = true
            },
            new EmployeeRolePresetItem
            {
                Id = Guid.Parse("2a2b3c4d-2222-2222-2222-222222222223"),
                PresetId = Guid.Parse("1a2b3c4d-2222-2222-2222-222222222222"),
                Name = "Coordinator",
                Description = "Schedules tasks and supports delivery.",
                SortOrder = 3,
                CreatedAt = createdAt,
                IsActive = true
            },
            new EmployeeRolePresetItem
            {
                Id = Guid.Parse("2a2b3c4d-2222-2222-2222-222222222224"),
                PresetId = Guid.Parse("1a2b3c4d-2222-2222-2222-222222222222"),
                Name = "Admin",
                Description = "Operations and billing support.",
                SortOrder = 4,
                CreatedAt = createdAt,
                IsActive = true
            },
            new EmployeeRolePresetItem
            {
                Id = Guid.Parse("2a2b3c4d-3333-3333-3333-333333333331"),
                PresetId = Guid.Parse("1a2b3c4d-3333-3333-3333-333333333333"),
                Name = "Consultant",
                Description = "Client-facing delivery specialist.",
                SortOrder = 1,
                CreatedAt = createdAt,
                IsActive = true
            },
            new EmployeeRolePresetItem
            {
                Id = Guid.Parse("2a2b3c4d-3333-3333-3333-333333333332"),
                PresetId = Guid.Parse("1a2b3c4d-3333-3333-3333-333333333333"),
                Name = "Lead",
                Description = "Owns engagement delivery and quality.",
                SortOrder = 2,
                CreatedAt = createdAt,
                IsActive = true
            },
            new EmployeeRolePresetItem
            {
                Id = Guid.Parse("2a2b3c4d-3333-3333-3333-333333333333"),
                PresetId = Guid.Parse("1a2b3c4d-3333-3333-3333-333333333333"),
                Name = "Coordinator",
                Description = "Plans meetings and follow-ups.",
                SortOrder = 3,
                CreatedAt = createdAt,
                IsActive = true
            },
            new EmployeeRolePresetItem
            {
                Id = Guid.Parse("2a2b3c4d-3333-3333-3333-333333333334"),
                PresetId = Guid.Parse("1a2b3c4d-3333-3333-3333-333333333333"),
                Name = "Admin",
                Description = "Back-office support and billing.",
                SortOrder = 4,
                CreatedAt = createdAt,
                IsActive = true
            },
            new EmployeeRolePresetItem
            {
                Id = Guid.Parse("2a2b3c4d-4444-4444-4444-444444444441"),
                PresetId = Guid.Parse("1a2b3c4d-4444-4444-4444-444444444444"),
                Name = "Repair Tech",
                Description = "Executes diagnostics and repairs.",
                SortOrder = 1,
                CreatedAt = createdAt,
                IsActive = true
            },
            new EmployeeRolePresetItem
            {
                Id = Guid.Parse("2a2b3c4d-4444-4444-4444-444444444442"),
                PresetId = Guid.Parse("1a2b3c4d-4444-4444-4444-444444444444"),
                Name = "QA",
                Description = "Final testing and release approvals.",
                SortOrder = 2,
                CreatedAt = createdAt,
                IsActive = true
            },
            new EmployeeRolePresetItem
            {
                Id = Guid.Parse("2a2b3c4d-4444-4444-4444-444444444443"),
                PresetId = Guid.Parse("1a2b3c4d-4444-4444-4444-444444444444"),
                Name = "Service Advisor",
                Description = "Client intake and status updates.",
                SortOrder = 3,
                CreatedAt = createdAt,
                IsActive = true
            },
            new EmployeeRolePresetItem
            {
                Id = Guid.Parse("2a2b3c4d-4444-4444-4444-444444444444"),
                PresetId = Guid.Parse("1a2b3c4d-4444-4444-4444-444444444444"),
                Name = "Admin",
                Description = "Operations and billing support.",
                SortOrder = 4,
                CreatedAt = createdAt,
                IsActive = true
            }
        );
    }
}
