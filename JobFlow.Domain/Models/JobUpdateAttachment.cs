namespace JobFlow.Domain.Models;

public class JobUpdateAttachment : Entity
{
    public Guid JobUpdateId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public byte[] FileData { get; set; } = Array.Empty<byte>();

    public JobUpdate JobUpdate { get; set; } = null!;
}
