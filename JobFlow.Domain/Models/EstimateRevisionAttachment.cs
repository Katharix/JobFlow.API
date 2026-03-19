namespace JobFlow.Domain.Models;

public class EstimateRevisionAttachment : Entity
{
    public Guid EstimateRevisionRequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public byte[] FileData { get; set; } = Array.Empty<byte>();

    public EstimateRevisionRequest RevisionRequest { get; set; } = null!;
}
