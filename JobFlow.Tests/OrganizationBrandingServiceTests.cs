using JobFlow.Business.Services;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobFlow.Tests;

public class OrganizationBrandingServiceTests
{
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
        public TestDbContextFactory(DbContextOptions<JobFlowDbContext> options) => _options = options;
        public JobFlowDbContext CreateDbContext() => new JobFlowDbContext(_options);
    }

    private static async Task EnsureOrganizationAsync(JobFlowUnitOfWork unitOfWork, Guid orgId, string name)
    {
        var org = new Organization
        {
            Id = orgId,
            OrganizationTypeId = Guid.NewGuid(),
            OrganizationName = name,
            IsActive = true
        };
        await unitOfWork.RepositoryOf<Organization>().AddAsync(org);
        await unitOfWork.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByOrganizationIdAsync_ReturnsEmptyBranding_WhenNoneExists()
    {
        var unitOfWork = CreateUnitOfWork(nameof(GetByOrganizationIdAsync_ReturnsEmptyBranding_WhenNoneExists));
        var orgId = Guid.NewGuid();
        await EnsureOrganizationAsync(unitOfWork, orgId, "Test Org");

        var service = new OrganizationBrandingService(unitOfWork, NullLogger<OrganizationBrandingService>.Instance);

        var result = await service.GetByOrganizationIdAsync(orgId);

        Assert.True(result.IsSuccess);
        Assert.Equal(orgId, result.Value.OrganizationId);
        Assert.Null(result.Value.LogoUrl);
        Assert.Null(result.Value.PrimaryColor);
    }

    [Fact]
    public async Task GetByOrganizationIdAsync_ReturnsExistingBranding()
    {
        var unitOfWork = CreateUnitOfWork(nameof(GetByOrganizationIdAsync_ReturnsExistingBranding));
        var orgId = Guid.NewGuid();
        await EnsureOrganizationAsync(unitOfWork, orgId, "Branded Org");

        var branding = new OrganizationBranding
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            PrimaryColor = "#e91e63",
            SecondaryColor = "#607d8b",
            BusinessName = "My Brand",
            Tagline = "Best in class",
            FooterNote = "Thank you",
            LogoUrl = "https://cdn.example.com/logo.png",
            CreatedAt = DateTime.UtcNow
        };
        await unitOfWork.RepositoryOf<OrganizationBranding>().AddAsync(branding);
        await unitOfWork.SaveChangesAsync();

        var service = new OrganizationBrandingService(unitOfWork, NullLogger<OrganizationBrandingService>.Instance);

        var result = await service.GetByOrganizationIdAsync(orgId);

        Assert.True(result.IsSuccess);
        Assert.Equal("#e91e63", result.Value.PrimaryColor);
        Assert.Equal("My Brand", result.Value.BusinessName);
        Assert.Equal("https://cdn.example.com/logo.png", result.Value.LogoUrl);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_CreatesNewBranding()
    {
        var unitOfWork = CreateUnitOfWork(nameof(CreateOrUpdateAsync_CreatesNewBranding));
        var orgId = Guid.NewGuid();
        await EnsureOrganizationAsync(unitOfWork, orgId, "New Org");

        var service = new OrganizationBrandingService(unitOfWork, NullLogger<OrganizationBrandingService>.Instance);

        var model = new OrganizationBranding
        {
            OrganizationId = orgId,
            PrimaryColor = "#ff5722",
            BusinessName = "Fresh Brand",
            LogoUrl = "https://cdn.example.com/new-logo.png"
        };

        var result = await service.CreateOrUpdateAsync(model);

        Assert.True(result.IsSuccess);
        Assert.Equal("#ff5722", result.Value.PrimaryColor);

        // Verify persisted
        var fetched = await service.GetByOrganizationIdAsync(orgId);
        Assert.Equal("Fresh Brand", fetched.Value.BusinessName);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_UpdatesExistingBranding()
    {
        var unitOfWork = CreateUnitOfWork(nameof(CreateOrUpdateAsync_UpdatesExistingBranding));
        var orgId = Guid.NewGuid();
        await EnsureOrganizationAsync(unitOfWork, orgId, "Update Org");

        var existing = new OrganizationBranding
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            PrimaryColor = "#000000",
            BusinessName = "Old Name",
            CreatedAt = DateTime.UtcNow
        };
        await unitOfWork.RepositoryOf<OrganizationBranding>().AddAsync(existing);
        await unitOfWork.SaveChangesAsync();

        var service = new OrganizationBrandingService(unitOfWork, NullLogger<OrganizationBrandingService>.Instance);

        var update = new OrganizationBranding
        {
            OrganizationId = orgId,
            PrimaryColor = "#ffffff",
            SecondaryColor = "#333333",
            BusinessName = "New Name",
            Tagline = "Updated tagline",
            FooterNote = "Updated footer",
            LogoUrl = "https://cdn.example.com/updated-logo.png"
        };

        var result = await service.CreateOrUpdateAsync(update);

        Assert.True(result.IsSuccess);

        // Verify the update persisted
        var fetched = await service.GetByOrganizationIdAsync(orgId);
        Assert.Equal("#ffffff", fetched.Value.PrimaryColor);
        Assert.Equal("#333333", fetched.Value.SecondaryColor);
        Assert.Equal("New Name", fetched.Value.BusinessName);
        Assert.Equal("Updated tagline", fetched.Value.Tagline);
        Assert.Equal("Updated footer", fetched.Value.FooterNote);
        Assert.Equal("https://cdn.example.com/updated-logo.png", fetched.Value.LogoUrl);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_SetsCreatedAtOnNewBranding()
    {
        var unitOfWork = CreateUnitOfWork(nameof(CreateOrUpdateAsync_SetsCreatedAtOnNewBranding));
        var orgId = Guid.NewGuid();
        await EnsureOrganizationAsync(unitOfWork, orgId, "Timestamp Org");

        var service = new OrganizationBrandingService(unitOfWork, NullLogger<OrganizationBrandingService>.Instance);

        var before = DateTime.UtcNow;
        var model = new OrganizationBranding
        {
            OrganizationId = orgId,
            PrimaryColor = "#123456"
        };

        var result = await service.CreateOrUpdateAsync(model);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.CreatedAt >= before);
    }
}
