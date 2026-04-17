using JobFlow.Business.Models.DTOs;

namespace JobFlow.Business.Services.ServiceInterfaces;

public interface IReportService
{
    Task<Result<ReportDashboardDto>> GetDashboardAsync(Guid organizationId, DateTimeOffset startDate, DateTimeOffset endDate);
}
