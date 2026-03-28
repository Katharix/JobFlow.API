namespace JobFlow.Domain.Models;

public class ClientImportUploadRow : Entity
{
    public Guid ClientImportUploadSessionId { get; set; }
    public int RowNumber { get; set; }
    public string RowDataJson { get; set; } = string.Empty;

    public virtual ClientImportUploadSession Session { get; set; } = null!;
}
