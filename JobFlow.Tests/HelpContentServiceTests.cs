using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using JobFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobFlow.Tests;

public class HelpContentServiceTests
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

    // ── CreateArticle ─────────────────────────────────────

    [Fact]
    public async Task CreateArticleAsync_Success()
    {
        var uow = CreateUnitOfWork(nameof(CreateArticleAsync_Success));
        var svc = new HelpContentService(uow);

        var request = new HelpArticleCreateRequest(
            "Getting Started", "Intro", "Full guide content",
            HelpArticleType.Guide, HelpArticleCategory.GettingStarted,
            "onboarding", false, true, 1);

        var result = await svc.CreateArticleAsync(request, "admin@test.com");

        Assert.True(result.IsSuccess);
        Assert.Equal("Getting Started", result.Value.Title);
        Assert.True(result.Value.IsPublished);
        Assert.NotNull(result.Value.PublishedAt);
    }

    [Fact]
    public async Task CreateArticleAsync_FailsWhenTitleEmpty()
    {
        var uow = CreateUnitOfWork(nameof(CreateArticleAsync_FailsWhenTitleEmpty));
        var svc = new HelpContentService(uow);

        var request = new HelpArticleCreateRequest(
            "", null, "Content",
            HelpArticleType.Guide, HelpArticleCategory.GettingStarted,
            null, false, false, 0);

        var result = await svc.CreateArticleAsync(request, null);

        Assert.True(result.IsFailure);
        Assert.Contains("Title is required", result.Error.Description);
    }

    [Fact]
    public async Task CreateArticleAsync_FailsWhenTitleTooLong()
    {
        var uow = CreateUnitOfWork(nameof(CreateArticleAsync_FailsWhenTitleTooLong));
        var svc = new HelpContentService(uow);

        var longTitle = new string('A', 201);
        var request = new HelpArticleCreateRequest(
            longTitle, null, "Content",
            HelpArticleType.Guide, HelpArticleCategory.GettingStarted,
            null, false, false, 0);

        var result = await svc.CreateArticleAsync(request, null);

        Assert.True(result.IsFailure);
        Assert.Contains("200 characters", result.Error.Description);
    }

    [Fact]
    public async Task CreateArticleAsync_FailsWhenContentEmpty()
    {
        var uow = CreateUnitOfWork(nameof(CreateArticleAsync_FailsWhenContentEmpty));
        var svc = new HelpContentService(uow);

        var request = new HelpArticleCreateRequest(
            "Valid Title", null, "",
            HelpArticleType.Guide, HelpArticleCategory.GettingStarted,
            null, false, false, 0);

        var result = await svc.CreateArticleAsync(request, null);

        Assert.True(result.IsFailure);
        Assert.Contains("Content is required", result.Error.Description);
    }

    [Fact]
    public async Task CreateArticleAsync_DoesNotSetPublishedAt_WhenNotPublished()
    {
        var uow = CreateUnitOfWork(nameof(CreateArticleAsync_DoesNotSetPublishedAt_WhenNotPublished));
        var svc = new HelpContentService(uow);

        var request = new HelpArticleCreateRequest(
            "Draft Article", null, "Draft content",
            HelpArticleType.Faq, HelpArticleCategory.Jobs,
            null, false, false, 0);

        var result = await svc.CreateArticleAsync(request, "admin@test.com");

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.IsPublished);
        Assert.Null(result.Value.PublishedAt);
    }

    // ── UpdateArticle ─────────────────────────────────────

    [Fact]
    public async Task UpdateArticleAsync_SetsPublishedAt_OnFirstPublish()
    {
        var uow = CreateUnitOfWork(nameof(UpdateArticleAsync_SetsPublishedAt_OnFirstPublish));
        var svc = new HelpContentService(uow);

        // Create as unpublished
        var createReq = new HelpArticleCreateRequest(
            "Draft", null, "Content",
            HelpArticleType.Guide, HelpArticleCategory.GettingStarted,
            null, false, false, 0);
        var created = await svc.CreateArticleAsync(createReq, "admin");
        Assert.True(created.IsSuccess);

        // Update to publish
        var updateReq = new HelpArticleUpdateRequest(
            created.Value.Id, "Published Title", null, "Updated content",
            HelpArticleType.Guide, HelpArticleCategory.GettingStarted,
            null, false, true, 1);
        var updated = await svc.UpdateArticleAsync(updateReq, "admin");

        Assert.True(updated.IsSuccess);
        Assert.True(updated.Value.IsPublished);
        Assert.NotNull(updated.Value.PublishedAt);
    }

    [Fact]
    public async Task UpdateArticleAsync_ReturnsNotFound_ForMissingArticle()
    {
        var uow = CreateUnitOfWork(nameof(UpdateArticleAsync_ReturnsNotFound_ForMissingArticle));
        var svc = new HelpContentService(uow);

        var request = new HelpArticleUpdateRequest(
            Guid.NewGuid(), "Title", null, "Content",
            HelpArticleType.Guide, HelpArticleCategory.GettingStarted,
            null, false, false, 0);

        var result = await svc.UpdateArticleAsync(request, null);

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error.Description);
    }

    // ── DeleteArticle ─────────────────────────────────────

    [Fact]
    public async Task DeleteArticleAsync_ReturnsNotFound_ForMissingArticle()
    {
        var uow = CreateUnitOfWork(nameof(DeleteArticleAsync_ReturnsNotFound_ForMissingArticle));
        var svc = new HelpContentService(uow);

        var result = await svc.DeleteArticleAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error.Description);
    }

    [Fact]
    public async Task DeleteArticleAsync_DeletesExistingArticle()
    {
        var uow = CreateUnitOfWork(nameof(DeleteArticleAsync_DeletesExistingArticle));
        var svc = new HelpContentService(uow);

        var createReq = new HelpArticleCreateRequest(
            "To Delete", null, "Content",
            HelpArticleType.Guide, HelpArticleCategory.GettingStarted,
            null, false, false, 0);
        var created = await svc.CreateArticleAsync(createReq, null);

        var deleteResult = await svc.DeleteArticleAsync(created.Value.Id);
        Assert.True(deleteResult.IsSuccess);

        var getResult = await svc.GetArticleByIdAsync(created.Value.Id);
        Assert.True(getResult.IsFailure);
    }

    // ── GetPublishedArticles ──────────────────────────────

    [Fact]
    public async Task GetPublishedArticlesAsync_ReturnsOnlyPublished()
    {
        var uow = CreateUnitOfWork(nameof(GetPublishedArticlesAsync_ReturnsOnlyPublished));
        var svc = new HelpContentService(uow);

        await svc.CreateArticleAsync(new HelpArticleCreateRequest(
            "Published", null, "Content", HelpArticleType.Guide,
            HelpArticleCategory.GettingStarted, null, false, true, 0), null);

        await svc.CreateArticleAsync(new HelpArticleCreateRequest(
            "Draft", null, "Content", HelpArticleType.Guide,
            HelpArticleCategory.GettingStarted, null, false, false, 0), null);

        var result = await svc.GetPublishedArticlesAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("Published", result.Value[0].Title);
    }

    // ── CreateChangelog ───────────────────────────────────

    [Fact]
    public async Task CreateChangelogEntryAsync_Success()
    {
        var uow = CreateUnitOfWork(nameof(CreateChangelogEntryAsync_Success));
        var svc = new HelpContentService(uow);

        var request = new ChangelogEntryCreateRequest(
            "v1.0 Release", "Initial release", "1.0",
            ChangelogCategory.Feature, true);

        var result = await svc.CreateChangelogEntryAsync(request, "admin");

        Assert.True(result.IsSuccess);
        Assert.Equal("v1.0 Release", result.Value.Title);
        Assert.True(result.Value.IsPublished);
        Assert.NotNull(result.Value.PublishedAt);
    }

    [Fact]
    public async Task CreateChangelogEntryAsync_FailsWhenTitleEmpty()
    {
        var uow = CreateUnitOfWork(nameof(CreateChangelogEntryAsync_FailsWhenTitleEmpty));
        var svc = new HelpContentService(uow);

        var request = new ChangelogEntryCreateRequest(
            "", null, null, ChangelogCategory.Fix, false);

        var result = await svc.CreateChangelogEntryAsync(request, null);

        Assert.True(result.IsFailure);
        Assert.Contains("Title is required", result.Error.Description);
    }

    [Fact]
    public async Task CreateChangelogEntryAsync_FailsWhenTitleTooLong()
    {
        var uow = CreateUnitOfWork(nameof(CreateChangelogEntryAsync_FailsWhenTitleTooLong));
        var svc = new HelpContentService(uow);

        var request = new ChangelogEntryCreateRequest(
            new string('Z', 201), null, null, ChangelogCategory.Fix, false);

        var result = await svc.CreateChangelogEntryAsync(request, null);

        Assert.True(result.IsFailure);
        Assert.Contains("200 characters", result.Error.Description);
    }

    // ── GetPublishedChangelog ─────────────────────────────

    [Fact]
    public async Task GetPublishedChangelogAsync_ReturnsOnlyPublished()
    {
        var uow = CreateUnitOfWork(nameof(GetPublishedChangelogAsync_ReturnsOnlyPublished));
        var svc = new HelpContentService(uow);

        await svc.CreateChangelogEntryAsync(new ChangelogEntryCreateRequest(
            "Published Entry", null, "1.0", ChangelogCategory.Feature, true), null);

        await svc.CreateChangelogEntryAsync(new ChangelogEntryCreateRequest(
            "Draft Entry", null, "1.1", ChangelogCategory.Improvement, false), null);

        var result = await svc.GetPublishedChangelogAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("Published Entry", result.Value[0].Title);
    }

    [Fact]
    public async Task GetPublishedChangelogAsync_OrdersByPublishedAtDescending()
    {
        var uow = CreateUnitOfWork(nameof(GetPublishedChangelogAsync_OrdersByPublishedAtDescending));
        var svc = new HelpContentService(uow);

        // Create two published entries (they'll get distinct PublishedAt values)
        await svc.CreateChangelogEntryAsync(new ChangelogEntryCreateRequest(
            "First", null, "1.0", ChangelogCategory.Feature, true), null);

        await svc.CreateChangelogEntryAsync(new ChangelogEntryCreateRequest(
            "Second", null, "2.0", ChangelogCategory.Feature, true), null);

        var result = await svc.GetPublishedChangelogAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        // Most recent should come first
        Assert.Equal("Second", result.Value[0].Title);
    }

    // ── DeleteChangelog ───────────────────────────────────

    [Fact]
    public async Task DeleteChangelogEntryAsync_ReturnsNotFound_ForMissingEntry()
    {
        var uow = CreateUnitOfWork(nameof(DeleteChangelogEntryAsync_ReturnsNotFound_ForMissingEntry));
        var svc = new HelpContentService(uow);

        var result = await svc.DeleteChangelogEntryAsync(Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error.Description);
    }

    // ── UpdateChangelog ───────────────────────────────────

    [Fact]
    public async Task UpdateChangelogEntryAsync_SetsPublishedAt_OnFirstPublish()
    {
        var uow = CreateUnitOfWork(nameof(UpdateChangelogEntryAsync_SetsPublishedAt_OnFirstPublish));
        var svc = new HelpContentService(uow);

        var created = await svc.CreateChangelogEntryAsync(new ChangelogEntryCreateRequest(
            "Draft", null, "0.1", ChangelogCategory.Fix, false), null);
        Assert.True(created.IsSuccess);

        var updateReq = new ChangelogEntryUpdateRequest(
            created.Value.Id, "Published", "desc", "1.0",
            ChangelogCategory.Fix, true);
        var updated = await svc.UpdateChangelogEntryAsync(updateReq, "admin");

        Assert.True(updated.IsSuccess);
        Assert.True(updated.Value.IsPublished);
        Assert.NotNull(updated.Value.PublishedAt);
    }

    [Fact]
    public async Task UpdateChangelogEntryAsync_ReturnsNotFound_ForMissingEntry()
    {
        var uow = CreateUnitOfWork(nameof(UpdateChangelogEntryAsync_ReturnsNotFound_ForMissingEntry));
        var svc = new HelpContentService(uow);

        var request = new ChangelogEntryUpdateRequest(
            Guid.NewGuid(), "Title", null, null,
            ChangelogCategory.Feature, false);

        var result = await svc.UpdateChangelogEntryAsync(request, null);

        Assert.True(result.IsFailure);
        Assert.Contains("not found", result.Error.Description);
    }
}
