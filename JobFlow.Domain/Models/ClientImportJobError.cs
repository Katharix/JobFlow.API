namespace JobFlow.Domain.Models;

public class ClientImportJobError : Entity
{
    public Guid ClientImportJobId { get; set; }
    public int RowNumber { get; set; }
    public string Message { get; set; } = string.Empty;

    public virtual ClientImportJob ClientImportJob { get; set; }
}
