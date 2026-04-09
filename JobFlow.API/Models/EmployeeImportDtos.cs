namespace JobFlow.API.Models;

public static class EmployeeImportTargetFields
{
    public const string Ignore = "Ignore";
    public const string FirstName = "FirstName";
    public const string LastName = "LastName";
    public const string FullName = "FullName";
    public const string Email = "Email";
    public const string PhoneNumber = "PhoneNumber";

    public static readonly string[] All =
    [
        Ignore,
        FirstName,
        LastName,
        FullName,
        Email,
        PhoneNumber
    ];
}

public sealed class EmployeeImportPreviewResponse
{
    public string UploadToken { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = "csv";
    public IReadOnlyList<string> SourceColumns { get; set; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string?> SuggestedMappings { get; set; } = new Dictionary<string, string?>();
    public IReadOnlyList<Dictionary<string, string?>> PreviewRows { get; set; } = Array.Empty<Dictionary<string, string?>>();
    public IReadOnlyList<string> SupportedTargetFields { get; set; } = EmployeeImportTargetFields.All;
    public int TotalRows { get; set; }
}

public sealed class PreviewEmployeeImportRequest
{
    public IFormFile? File { get; set; }
    public string? SourceSystem { get; set; }
}

public sealed class StartEmployeeImportRequest
{
    public string UploadToken { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = "csv";
    public Dictionary<string, string?> ColumnMappings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class StartEmployeeImportResponse
{
    public string JobId { get; set; } = string.Empty;
}

public sealed class EmployeeImportErrorItem
{
    public int RowNumber { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class EmployeeImportJobStatusResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = "queued";
    public string SourceSystem { get; set; } = "csv";
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SucceededRows { get; set; }
    public int FailedRows { get; set; }
    public string? ErrorMessage { get; set; }
    public IReadOnlyList<EmployeeImportErrorItem> Errors { get; set; } = Array.Empty<EmployeeImportErrorItem>();
}
