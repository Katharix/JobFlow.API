using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobFlow.Tests;

public class EmployeeRolePresetServiceTests
{
    [Fact]
    public async Task GetAvailablePresets_ReturnsOrgAndMatchingSystem()
    {
        var orgId = Guid.NewGuid();
        var otherOrgId = Guid.NewGuid();
        var unitOfWork = CreateUnitOfWork(nameof(GetAvailablePresets_ReturnsOrgAndMatchingSystem));
        await EnsureOrganizationAsync(unitOfWork, orgId, "Org One");
        await EnsureOrganizationAsync(unitOfWork, otherOrgId, "Org Two");
        await SeedPresetsAsync(unitOfWork, orgId, otherOrgId);

        var service = new EmployeeRolePresetService(NullLogger<EmployeeRolePresetService>.Instance, unitOfWork);

        var result = await service.GetAvailablePresetsAsync(orgId, "home-services");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count());
        Assert.Contains(result.Value, preset => preset.Name == "Org Preset");
        Assert.Contains(result.Value, preset => preset.Name == "Home Services");
        Assert.DoesNotContain(result.Value, preset => preset.Name == "Creative");
    }

    [Fact]
    public async Task ApplyPreset_CreatesRolesAndCounts()
    {
        var orgId = Guid.NewGuid();
        var unitOfWork = CreateUnitOfWork(nameof(ApplyPreset_CreatesRolesAndCounts));
        var presetId = Guid.NewGuid();

        await EnsureOrganizationAsync(unitOfWork, orgId, "Preset Org");

        await AddPresetAsync(unitOfWork,
            new EmployeeRolePreset
            {
                Id = presetId,
                Name = "Starter",
                IsSystem = true,
                IndustryKey = "home-services"
            },
            new List<EmployeeRolePresetItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    PresetId = presetId,
                    Name = "Technician",
                    Description = "Field technician",
                    SortOrder = 1
                }
            });

        var service = new EmployeeRolePresetService(NullLogger<EmployeeRolePresetService>.Instance, unitOfWork);
        var result = await service.ApplyPresetAsync(orgId, presetId, true);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Created);
        Assert.Equal(0, result.Value.Updated);
        Assert.Equal(0, result.Value.Skipped);

        var roles = await unitOfWork.RepositoryOf<EmployeeRole>()
            .Query()
            .Where(role => role.OrganizationId == orgId)
            .ToListAsync();

        Assert.Single(roles);
        Assert.Equal("TECHNICIAN", roles[0].Name);
        Assert.Equal("Field technician", roles[0].Description);
    }

    [Fact]
    public async Task ApplyPreset_SkipsExisting_WhenOverwriteFalse()
    {
        var orgId = Guid.NewGuid();
        var unitOfWork = CreateUnitOfWork(nameof(ApplyPreset_SkipsExisting_WhenOverwriteFalse));
        var presetId = Guid.NewGuid();

        await EnsureOrganizationAsync(unitOfWork, orgId, "Preset Org");
        await AddRoleAsync(unitOfWork, orgId, "TECHNICIAN", "Existing description");

        await AddPresetAsync(unitOfWork,
            new EmployeeRolePreset
            {
                Id = presetId,
                Name = "Starter",
                IsSystem = true,
                IndustryKey = "home-services"
            },
            new List<EmployeeRolePresetItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    PresetId = presetId,
                    Name = "Technician",
                    Description = "Updated description",
                    SortOrder = 1
                }
            });

        var service = new EmployeeRolePresetService(NullLogger<EmployeeRolePresetService>.Instance, unitOfWork);
        var result = await service.ApplyPresetAsync(orgId, presetId, false);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Created);
        Assert.Equal(0, result.Value.Updated);
        Assert.Equal(1, result.Value.Skipped);

        var roles = await unitOfWork.RepositoryOf<EmployeeRole>()
            .Query()
            .Where(role => role.OrganizationId == orgId)
            .ToListAsync();

        Assert.Single(roles);
        Assert.Equal("Existing description", roles[0].Description);
    }

    [Fact]
    public async Task ApplyPreset_UpdatesExisting_WhenOverwriteTrue()
    {
        var orgId = Guid.NewGuid();
        var unitOfWork = CreateUnitOfWork(nameof(ApplyPreset_UpdatesExisting_WhenOverwriteTrue));
        var presetId = Guid.NewGuid();

        await EnsureOrganizationAsync(unitOfWork, orgId, "Preset Org");
        await AddRoleAsync(unitOfWork, orgId, "TECHNICIAN", "Existing description");

        await AddPresetAsync(unitOfWork,
            new EmployeeRolePreset
            {
                Id = presetId,
                Name = "Starter",
                IsSystem = true,
                IndustryKey = "home-services"
            },
            new List<EmployeeRolePresetItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    PresetId = presetId,
                    Name = "Technician",
                    Description = "Updated description",
                    SortOrder = 1
                }
            });

        var service = new EmployeeRolePresetService(NullLogger<EmployeeRolePresetService>.Instance, unitOfWork);
        var result = await service.ApplyPresetAsync(orgId, presetId, true);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Created);
        Assert.Equal(1, result.Value.Updated);
        Assert.Equal(0, result.Value.Skipped);

        var roles = await unitOfWork.RepositoryOf<EmployeeRole>()
            .Query()
            .Where(role => role.OrganizationId == orgId)
            .ToListAsync();

        Assert.Single(roles);
        Assert.Equal("Updated description", roles[0].Description);
    }

    private static async Task SeedPresetsAsync(JobFlowUnitOfWork unitOfWork, Guid orgId, Guid otherOrgId)
    {
        await AddPresetAsync(unitOfWork,
            new EmployeeRolePreset
            {
                Id = Guid.NewGuid(),
                Name = "Home Services",
                IsSystem = true,
                IndustryKey = "home-services"
            },
            new List<EmployeeRolePresetItem>());

        await AddPresetAsync(unitOfWork,
            new EmployeeRolePreset
            {
                Id = Guid.NewGuid(),
                Name = "Creative",
                IsSystem = true,
                IndustryKey = "creative"
            },
            new List<EmployeeRolePresetItem>());

        await AddPresetAsync(unitOfWork,
            new EmployeeRolePreset
            {
                Id = Guid.NewGuid(),
                Name = "Org Preset",
                IsSystem = false,
                OrganizationId = orgId
            },
            new List<EmployeeRolePresetItem>());

        await AddPresetAsync(unitOfWork,
            new EmployeeRolePreset
            {
                Id = Guid.NewGuid(),
                Name = "Other Org Preset",
                IsSystem = false,
                OrganizationId = otherOrgId
            },
            new List<EmployeeRolePresetItem>());
    }

    private static async Task AddPresetAsync(
        JobFlowUnitOfWork unitOfWork,
        EmployeeRolePreset preset,
        List<EmployeeRolePresetItem> items)
    {
        preset.IsActive = true;
        for (var index = 0; index < items.Count; index += 1)
        {
            var item = items[index];
            item.PresetId = item.PresetId == Guid.Empty ? preset.Id : item.PresetId;
            item.SortOrder = item.SortOrder == 0 ? index + 1 : item.SortOrder;
            item.IsActive = true;
        }

        preset.Items = items;
        await unitOfWork.RepositoryOf<EmployeeRolePreset>().AddAsync(preset);
        await unitOfWork.SaveChangesAsync();
    }

    private static async Task EnsureOrganizationAsync(JobFlowUnitOfWork unitOfWork, Guid organizationId, string name)
    {
        var organization = new Organization
        {
            Id = organizationId,
            OrganizationTypeId = Guid.NewGuid(),
            OrganizationName = name,
            IsActive = true
        };

        await unitOfWork.RepositoryOf<Organization>().AddAsync(organization);
        await unitOfWork.SaveChangesAsync();
    }

    private static async Task AddRoleAsync(JobFlowUnitOfWork unitOfWork, Guid organizationId, string name, string? description)
    {
        var role = new EmployeeRole
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = name,
            Description = description
        };

        await unitOfWork.RepositoryOf<EmployeeRole>().AddAsync(role);
        await unitOfWork.SaveChangesAsync();
    }

    private static JobFlowUnitOfWork CreateUnitOfWork(string databaseName)
    {
        var options = new DbContextOptionsBuilder<JobFlowDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var factory = new TestDbContextFactory(options);
        return new JobFlowUnitOfWork(NullLogger<JobFlowUnitOfWork>.Instance, factory);
    }

    private sealed class TestDbContextFactory : IDbContextFactory<JobFlowDbContext>
    {
        private readonly DbContextOptions<JobFlowDbContext> _options;

        public TestDbContextFactory(DbContextOptions<JobFlowDbContext> options)
        {
            _options = options;
        }

        public JobFlowDbContext CreateDbContext()
        {
            return new JobFlowDbContext(_options);
        }
    }
}
