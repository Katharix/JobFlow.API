using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IHelpContentService
{
    Task<Result<List<HelpArticleDto>>> GetPublishedArticlesAsync();
    Task<Result<List<HelpArticleDto>>> GetAllArticlesAsync();
    Task<Result<HelpArticleDto>> GetArticleByIdAsync(Guid id);
    Task<Result<HelpArticleDto>> CreateArticleAsync(HelpArticleCreateRequest request, string? createdBy);
    Task<Result<HelpArticleDto>> UpdateArticleAsync(HelpArticleUpdateRequest request, string? updatedBy);
    Task<Result> DeleteArticleAsync(Guid id);

    Task<Result<List<ChangelogEntryDto>>> GetPublishedChangelogAsync();
    Task<Result<List<ChangelogEntryDto>>> GetAllChangelogAsync();
    Task<Result<ChangelogEntryDto>> CreateChangelogEntryAsync(ChangelogEntryCreateRequest request, string? createdBy);
    Task<Result<ChangelogEntryDto>> UpdateChangelogEntryAsync(ChangelogEntryUpdateRequest request, string? updatedBy);
    Task<Result> DeleteChangelogEntryAsync(Guid id);

    Task<Result> SeedHelpContentAsync(string? createdBy);
}
