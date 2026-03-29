using JobFlow.Domain.Enums;

namespace JobFlow.Business.Models.DTOs;

public class DispatchBoardDto
{
    public DateTimeOffset RangeStart { get; set; }
    public DateTimeOffset RangeEnd { get; set; }

    public List<EmployeeDto> Employees { get; set; } = new();
    public List<AssignmentDto> Assignments { get; set; } = new();
    public List<DispatchUnscheduledJobDto> UnscheduledJobs { get; set; } = new();
}

public class DispatchUnscheduledJobDto
{
    public Guid JobId { get; set; }
    public string? JobTitle { get; set; }
    public string? ClientName { get; set; }
    public JobLifecycleStatus JobLifecycleStatus { get; set; }
    public string? Notes { get; set; }
}
