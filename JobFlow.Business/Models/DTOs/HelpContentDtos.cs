using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;

public record HelpArticleDto(
    Guid Id,
    string Title,
    string? Summary,
    string Content,
    HelpArticleType ArticleType,
    HelpArticleCategory Category,
    string? Tags,
    bool IsFeatured,
    bool IsPublished,
    int SortOrder,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt);

public record HelpArticleCreateRequest(
    string Title,
    string? Summary,
    string Content,
    HelpArticleType ArticleType,
    HelpArticleCategory Category,
    string? Tags,
    bool IsFeatured,
    bool IsPublished,
    int SortOrder);

public record HelpArticleUpdateRequest(
    Guid Id,
    string Title,
    string? Summary,
    string Content,
    HelpArticleType ArticleType,
    HelpArticleCategory Category,
    string? Tags,
    bool IsFeatured,
    bool IsPublished,
    int SortOrder);

public record ChangelogEntryDto(
    Guid Id,
    string Title,
    string? Description,
    string? Version,
    ChangelogCategory Category,
    bool IsPublished,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt);

public record ChangelogEntryCreateRequest(
    string Title,
    string? Description,
    string? Version,
    ChangelogCategory Category,
    bool IsPublished);

public record ChangelogEntryUpdateRequest(
    Guid Id,
    string Title,
    string? Description,
    string? Version,
    ChangelogCategory Category,
    bool IsPublished);
