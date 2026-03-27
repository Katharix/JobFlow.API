namespace JobFlow.Domain.Models;

public class DataExportJob : Entity
{
    public Guid OrganizationId { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string Status { get; set; } = "queued";
    public string? ErrorMessage { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public byte[]? FileContent { get; set; }
    public int DownloadCount { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }

    public virtual Organization Organization { get; set; }
}