using JobFlow.Business.DI;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.Business.Services;

[ScopedService]
public class FeatureUsageService : IFeatureUsageService
{
    private readonly IRepository<Employee> _employees;
    private readonly IRepository<PriceBookItem> _priceBookItems;

    public FeatureUsageService(IUnitOfWork unitOfWork)
    {
        _employees = unitOfWork.RepositoryOf<Employee>();
        _priceBookItems = unitOfWork.RepositoryOf<PriceBookItem>();
    }

    public async Task<FeatureUsageSummary> GetAsync(Guid organizationId)
    {
        var employeeCount = await _employees.Query()
            .Where(e => e.OrganizationId == organizationId && e.IsActive)
            .CountAsync();

        var priceBookCount = await _priceBookItems.Query()
            .Where(p => p.OrganizationId == organizationId)
            .CountAsync();

        return new FeatureUsageSummary
        {
            EmployeeCount = employeeCount,
            PriceBookItemCount = priceBookCount,
            RecommendedPlanKey = RecommendPlan(employeeCount, priceBookCount)
        };
    }

    private static string RecommendPlan(int employeeCount, int priceBookCount)
    {
        if (employeeCount >= 5) return "max";
        if (employeeCount > 0 || priceBookCount > 0) return "flow";
        return "go";
    }
}
