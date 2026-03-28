namespace JobFlow.Domain.Models;

public class Order : Entity
{
    public Guid OrganizationClientId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Completed, Canceled
    public string Notes { get; set; } = string.Empty;
    public virtual OrganizationClient OrganizationClient { get; set; } = null!;
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<AssignmentOrder> AssignmentOrders { get; set; } = new List<AssignmentOrder>();
}