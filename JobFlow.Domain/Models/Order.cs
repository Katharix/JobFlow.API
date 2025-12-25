namespace JobFlow.Domain.Models;

public class Order : Entity
{
    public Guid OrganizationClientId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } // Pending, Completed, Canceled
    public string Notes { get; set; }
    public virtual OrganizationClient OrganizationClient { get; set; }
    public virtual ICollection<Invoice> Invoices { get; set; }
    public virtual ICollection<JobOrder> JobOrders { get; set; } = new List<JobOrder>();
}