namespace JobFlow.Business.Models.DTOs;

public class OnboardingIndustryDefaultsDto
{
    public string OrgTypeName { get; set; } = string.Empty;
    public string TemplateSuggestionName { get; set; } = string.Empty;
    public int PaymentTermsDays { get; set; }
    public IReadOnlyList<OnboardingIndustryServiceDto> Services { get; set; } = [];
}

public class OnboardingIndustryServiceDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
