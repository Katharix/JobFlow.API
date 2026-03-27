namespace JobFlow.Domain.Models;

public class ClientImportJob : Entity
{
    public Guid OrganizationId { get; set; }
    public string SourceSystem { get; set; } = "csv";
    public string Status { get; set; } = "queued";
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SucceededRows { get; set; }
    public int FailedRows { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public virtual Organization Organization { get; set; }
    public virtual ICollection<ClientImportJobError> Errors { get; set; } = new List<ClientImportJobError>();
}
