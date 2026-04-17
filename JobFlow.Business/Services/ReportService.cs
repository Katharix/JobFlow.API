using JobFlow.Business.DI;
using JobFlow.Business.Models.DTOs;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Enums;
using JobFlow.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobFlow.Business.Services;

[ScopedService]
public class ReportService : IReportService
{
    private readonly IRepository<Invoice> _invoiceRepo;
    private readonly IRepository<Job> _jobRepo;
    private readonly IRepository<Assignment> _assignmentRepo;
    private readonly IRepository<AssignmentAssignee> _assigneeRepo;
    private readonly IRepository<Employee> _employeeRepo;
    private readonly IRepository<OrganizationClient> _clientRepo;
    private readonly ILogger<ReportService> _logger;

    public ReportService(IUnitOfWork unitOfWork, ILogger<ReportService> logger)
    {
        _invoiceRepo = unitOfWork.RepositoryOf<Invoice>();
        _jobRepo = unitOfWork.RepositoryOf<Job>();
        _assignmentRepo = unitOfWork.RepositoryOf<Assignment>();
        _assigneeRepo = unitOfWork.RepositoryOf<AssignmentAssignee>();
        _employeeRepo = unitOfWork.RepositoryOf<Employee>();
        _clientRepo = unitOfWork.RepositoryOf<OrganizationClient>();
        _logger = logger;
    }

    public async Task<Result<ReportDashboardDto>> GetDashboardAsync(
        Guid organizationId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var dashboard = new ReportDashboardDto
        {
            Revenue = await GetRevenueOverviewAsync(organizationId, startDate, endDate),
            JobPerformance = await GetJobPerformanceAsync(organizationId, startDate, endDate),
            EmployeeUtilization = await GetEmployeeUtilizationAsync(organizationId, startDate, endDate),
            TopClients = await GetTopClientsAsync(organizationId, startDate, endDate)
        };

        return Result.Success(dashboard);
    }

    private async Task<RevenueOverviewDto> GetRevenueOverviewAsync(
        Guid organizationId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var invoices = await _invoiceRepo.QueryWithNoTracking()
            .Where(i => i.OrganizationId == organizationId
                        && i.InvoiceDate >= startDate
                        && i.InvoiceDate <= endDate)
            .ToListAsync();

        var monthlyTrend = invoices
            .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyRevenueDto
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                Billed = g.Sum(i => i.TotalAmount),
                Collected = g.Sum(i => i.AmountPaid)
            })
            .ToList();

        return new RevenueOverviewDto
        {
            TotalBilled = invoices.Sum(i => i.TotalAmount),
            TotalCollected = invoices.Sum(i => i.AmountPaid),
            TotalOutstanding = invoices.Sum(i => i.BalanceDue),
            InvoiceCount = invoices.Count,
            PaidCount = invoices.Count(i => i.Status == InvoiceStatus.Paid),
            OverdueCount = invoices.Count(i => i.Status == InvoiceStatus.Overdue),
            MonthlyTrend = monthlyTrend
        };
    }

    private async Task<JobPerformanceDto> GetJobPerformanceAsync(
        Guid organizationId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var jobs = await _jobRepo.QueryWithNoTracking()
            .Include(j => j.OrganizationClient)
            .Where(j => j.OrganizationClient.OrganizationId == organizationId
                        && j.CreatedAt >= startDate.DateTime
                        && j.CreatedAt <= endDate.DateTime)
            .ToListAsync();

        var completedJobs = jobs.Where(j => j.LifecycleStatus == JobLifecycleStatus.Completed).ToList();
        var avgDays = completedJobs.Any() && completedJobs.Any(j => j.UpdatedAt.HasValue)
            ? completedJobs
                .Where(j => j.UpdatedAt.HasValue)
                .Average(j => (j.UpdatedAt!.Value - j.CreatedAt).TotalDays)
            : 0;

        var totalExcludingDraft = jobs.Count(j => j.LifecycleStatus != JobLifecycleStatus.Draft);
        var completionRate = totalExcludingDraft > 0
            ? (double)completedJobs.Count / totalExcludingDraft * 100
            : 0;

        var monthlyTrend = jobs
            .GroupBy(j => new { j.CreatedAt.Year, j.CreatedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new MonthlyJobCountDto
            {
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                Created = g.Count(),
                Completed = g.Count(j => j.LifecycleStatus == JobLifecycleStatus.Completed)
            })
            .ToList();

        return new JobPerformanceDto
        {
            TotalJobs = jobs.Count,
            DraftCount = jobs.Count(j => j.LifecycleStatus == JobLifecycleStatus.Draft),
            BookedCount = jobs.Count(j => j.LifecycleStatus == JobLifecycleStatus.Booked),
            InProgressCount = jobs.Count(j => j.LifecycleStatus == JobLifecycleStatus.InProgress),
            CompletedCount = completedJobs.Count,
            CancelledCount = jobs.Count(j => j.LifecycleStatus == JobLifecycleStatus.Cancelled),
            AvgCompletionDays = Math.Round(avgDays, 1),
            CompletionRate = Math.Round(completionRate, 1),
            MonthlyTrend = monthlyTrend
        };
    }

    private async Task<List<EmployeeUtilizationDto>> GetEmployeeUtilizationAsync(
        Guid organizationId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var employees = await _employeeRepo.QueryWithNoTracking()
            .Where(e => e.OrganizationId == organizationId && e.IsActive)
            .ToListAsync();

        var employeeIds = employees.Select(e => e.Id).ToHashSet();

        var assignees = await _assigneeRepo.QueryWithNoTracking()
            .Include(aa => aa.Assignment)
            .Where(aa => employeeIds.Contains(aa.EmployeeId)
                         && aa.Assignment.ScheduledStart >= startDate
                         && aa.Assignment.ScheduledStart <= endDate)
            .ToListAsync();

        return employees.Select(emp =>
        {
            var empAssignees = assignees.Where(a => a.EmployeeId == emp.Id).ToList();

            var scheduledHours = empAssignees
                .Where(a => a.Assignment.ScheduledEnd.HasValue)
                .Sum(a => (a.Assignment.ScheduledEnd!.Value - a.Assignment.ScheduledStart).TotalHours);

            var actualHours = empAssignees
                .Where(a => a.Assignment.ActualStart.HasValue && a.Assignment.ActualEnd.HasValue)
                .Sum(a => (a.Assignment.ActualEnd!.Value - a.Assignment.ActualStart!.Value).TotalHours);

            return new EmployeeUtilizationDto
            {
                EmployeeId = emp.Id,
                EmployeeName = emp.FullName,
                ScheduledHours = Math.Round(scheduledHours, 1),
                ActualHours = Math.Round(actualHours, 1),
                AssignmentCount = empAssignees.Count,
                CompletedCount = empAssignees.Count(a => a.Assignment.Status == AssignmentStatus.Completed)
            };
        })
        .OrderByDescending(e => e.ActualHours)
        .Take(15)
        .ToList();
    }

    private async Task<List<ClientInsightDto>> GetTopClientsAsync(
        Guid organizationId, DateTimeOffset startDate, DateTimeOffset endDate)
    {
        var clients = await _clientRepo.QueryWithNoTracking()
            .Include(c => c.Jobs)
            .Where(c => c.OrganizationId == organizationId)
            .ToListAsync();

        var clientIds = clients.Select(c => c.Id).ToHashSet();

        var invoices = await _invoiceRepo.QueryWithNoTracking()
            .Where(i => i.OrganizationId == organizationId
                        && clientIds.Contains(i.OrganizationClientId)
                        && i.InvoiceDate >= startDate
                        && i.InvoiceDate <= endDate)
            .ToListAsync();

        return clients.Select(client =>
        {
            var clientInvoices = invoices
                .Where(i => i.OrganizationClientId == client.Id)
                .ToList();

            var jobCount = client.Jobs.Count(j =>
                j.CreatedAt >= startDate.DateTime && j.CreatedAt <= endDate.DateTime);

            return new ClientInsightDto
            {
                ClientId = client.Id,
                ClientName = client.ClientFullName(),
                JobCount = jobCount,
                TotalRevenue = clientInvoices.Sum(i => i.AmountPaid),
                OutstandingBalance = clientInvoices.Sum(i => i.BalanceDue)
            };
        })
        .Where(c => c.JobCount > 0 || c.TotalRevenue > 0)
        .OrderByDescending(c => c.TotalRevenue)
        .Take(10)
        .ToList();
    }
}
