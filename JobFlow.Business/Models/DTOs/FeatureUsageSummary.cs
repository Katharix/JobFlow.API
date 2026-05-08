namespace JobFlow.Business.Models.DTOs;

public class FeatureUsageSummary
{
    public int EmployeeCount { get; set; }
    public int PriceBookItemCount { get; set; }
    public string RecommendedPlanKey { get; set; } = "go";
}
