namespace JobFlow.Domain.Models;

public class AssignmentOrder
{
    public Guid AssignmentId { get; set; }
    public Guid OrderId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Assignment Assignment { get; set; } = null!;
    public virtual Order Order { get; set; } = null!;
}