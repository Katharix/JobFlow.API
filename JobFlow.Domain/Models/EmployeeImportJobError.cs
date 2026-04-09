namespace JobFlow.Domain.Models;

public class EmployeeImportJobError : Entity
{
    public Guid EmployeeImportJobId { get; set; }
    public int RowNumber { get; set; }
    public string Message { get; set; } = string.Empty;

    public virtual EmployeeImportJob EmployeeImportJob { get; set; } = null!;
}
