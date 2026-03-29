namespace JobFlow.API.Models;

public sealed class DataExportBundleDto
{
    public string ExportedAtUtc { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public IReadOnlyList<DataExportClientDto> Clients { get; set; } = Array.Empty<DataExportClientDto>();
    public IReadOnlyList<DataExportJobDto> Jobs { get; set; } = Array.Empty<DataExportJobDto>();
    public IReadOnlyList<DataExportInvoiceDto> Invoices { get; set; } = Array.Empty<DataExportInvoiceDto>();
    public IReadOnlyList<DataExportEmployeeDto> Employees { get; set; } = Array.Empty<DataExportEmployeeDto>();
}

public sealed class DataExportClientDto
{
    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? EmailAddress { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
}

public sealed class DataExportJobDto
{
    public Guid Id { get; set; }
    public Guid OrganizationClientId { get; set; }
    public string? Title { get; set; }
    public string? Comments { get; set; }
    public string LifecycleStatus { get; set; } = string.Empty;
    public string? InvoicingWorkflow { get; set; }
}

public sealed class DataExportInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid OrganizationClientId { get; set; }
    public Guid? JobId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class DataExportEmployeeDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class StartDataExportJobResponse
{
    public string JobId { get; set; } = string.Empty;
}

public sealed class DataExportJobStatusResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public int DownloadCount { get; set; }
}
