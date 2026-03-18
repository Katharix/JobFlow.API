namespace JobFlow.Domain.Models;

public interface ISoftDeletable
{
    bool IsActive { get; set; }
    DateTime? DeactivatedAtUtc { get; set; }
}
