namespace JobFlow.Business.Models.DTOs;

public class ReportDashboardDto
{
    public RevenueOverviewDto Revenue { get; set; } = new();
    public JobPerformanceDto JobPerformance { get; set; } = new();
    public List<EmployeeUtilizationDto> EmployeeUtilization { get; set; } = new();
    public List<ClientInsightDto> TopClients { get; set; } = new();
}

public class RevenueOverviewDto
{
    public decimal TotalBilled { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalOutstanding { get; set; }
    public int InvoiceCount { get; set; }
    public int PaidCount { get; set; }
    public int OverdueCount { get; set; }
    public List<MonthlyRevenueDto> MonthlyTrend { get; set; } = new();
}

public class MonthlyRevenueDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Billed { get; set; }
    public decimal Collected { get; set; }
}

public class JobPerformanceDto
{
    public int TotalJobs { get; set; }
    public int DraftCount { get; set; }
    public int BookedCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public double AvgCompletionDays { get; set; }
    public double CompletionRate { get; set; }
    public List<MonthlyJobCountDto> MonthlyTrend { get; set; } = new();
}

public class MonthlyJobCountDto
{
    public string Month { get; set; } = string.Empty;
    public int Created { get; set; }
    public int Completed { get; set; }
}

public class EmployeeUtilizationDto
{
    public Guid EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public double ScheduledHours { get; set; }
    public double ActualHours { get; set; }
    public int AssignmentCount { get; set; }
    public int CompletedCount { get; set; }
}

public class ClientInsightDto
{
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public int JobCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal OutstandingBalance { get; set; }
}
