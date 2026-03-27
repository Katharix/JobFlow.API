namespace JobFlow.API.Models;

public static class ClientImportTargetFields
{
    public const string Ignore = "Ignore";
    public const string FirstName = "FirstName";
    public const string LastName = "LastName";
    public const string FullName = "FullName";
    public const string EmailAddress = "EmailAddress";
    public const string PhoneNumber = "PhoneNumber";
    public const string Address1 = "Address1";
    public const string Address2 = "Address2";
    public const string City = "City";
    public const string State = "State";
    public const string ZipCode = "ZipCode";

    public static readonly string[] All =
    [
        Ignore,
        FirstName,
        LastName,
        FullName,
        EmailAddress,
        PhoneNumber,
        Address1,
        Address2,
        City,
        State,
        ZipCode
    ];
}

public sealed class ClientImportPreviewResponse
{
    public string UploadToken { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = "csv";
    public IReadOnlyList<string> SourceColumns { get; set; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string?> SuggestedMappings { get; set; } = new Dictionary<string, string?>();
    public IReadOnlyList<Dictionary<string, string?>> PreviewRows { get; set; } = Array.Empty<Dictionary<string, string?>>();
    public IReadOnlyList<string> SupportedTargetFields { get; set; } = ClientImportTargetFields.All;
    public int TotalRows { get; set; }
}

public sealed class PreviewClientImportRequest
{
    public IFormFile? File { get; set; }
    public string? SourceSystem { get; set; }
}

public sealed class StartClientImportRequest
{
    public string UploadToken { get; set; } = string.Empty;
    public string SourceSystem { get; set; } = "csv";
    public Dictionary<string, string?> ColumnMappings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class StartClientImportResponse
{
    public string JobId { get; set; } = string.Empty;
}

public sealed class ClientImportErrorItem
{
    public int RowNumber { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class ClientImportJobStatusResponse
{
    public string JobId { get; set; } = string.Empty;
    public string Status { get; set; } = "queued";
    public string SourceSystem { get; set; } = "csv";
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SucceededRows { get; set; }
    public int FailedRows { get; set; }
    public string? ErrorMessage { get; set; }
    public IReadOnlyList<ClientImportErrorItem> Errors { get; set; } = Array.Empty<ClientImportErrorItem>();
}
