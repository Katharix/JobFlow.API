using JobFlow.Domain.Enums;

namespace JobFlow.Domain.Models;

public class OrganizationInvoicingSettings : Entity
{
    public Guid OrganizationId { get; set; }
    public InvoicingWorkflow DefaultWorkflow { get; set; } = InvoicingWorkflow.SendInvoice;

    public bool DepositRequired { get; set; }
    public decimal DepositPercentage { get; set; }

    /// <summary>Net payment terms in days. 0 = due on receipt.</summary>
    public int PaymentTermsDays { get; set; } = 0;
}