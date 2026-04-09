namespace JobFlow.Domain.Models;

public class EmployeeImportUploadSession : Entity
{
    public Guid OrganizationId { get; set; }
    public string SourceSystem { get; set; } = "csv";
    public string Status { get; set; } = "active";
    public int TotalRows { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }

    public virtual Organization Organization { get; set; } = null!;
    public virtual ICollection<EmployeeImportUploadRow> Rows { get; set; } = new List<EmployeeImportUploadRow>();
}
