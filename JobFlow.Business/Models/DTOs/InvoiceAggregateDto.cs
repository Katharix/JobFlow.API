namespace JobFlow.Business.Models.DTOs;

public class InvoiceAggregateDto
{
    public int InvoiceCount { get; set; }
    public int DraftCount { get; set; }
    public int SentCount { get; set; }
    public int PaidCount { get; set; }
    public int OverdueCount { get; set; }
    public int RefundedCount { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal BalanceDue { get; set; }
    public decimal Outstanding { get; set; }
}
