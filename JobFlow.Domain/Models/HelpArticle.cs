using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class HelpArticle : Entity
{
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public HelpArticleType ArticleType { get; set; } = HelpArticleType.Guide;
    public HelpArticleCategory Category { get; set; } = HelpArticleCategory.GettingStarted;
    public string? Tags { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsPublished { get; set; }
    public int SortOrder { get; set; }
    public string? PublishedBy { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
}
