namespace JobFlow.Domain.Models;

public class EmployeeImportUploadRow : Entity
{
    public Guid EmployeeImportUploadSessionId { get; set; }
    public int RowNumber { get; set; }
    public string RowDataJson { get; set; } = string.Empty;

    public virtual EmployeeImportUploadSession Session { get; set; } = null!;
}
