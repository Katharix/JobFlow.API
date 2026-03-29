namespace JobFlow.Domain.Models;

public abstract class Entity : ISoftDeletable
{
    public Guid Id { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? DeactivatedAtUtc { get; set; }
}