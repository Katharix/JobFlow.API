namespace JobFlow.Business.Models.DTOs;

public class FinancialSummaryDto
{
    public decimal GrossCollected { get; set; }
    public decimal Refunded { get; set; }
    public decimal NetCollected { get; set; }
    public decimal MonthCollected { get; set; }
    public decimal Outstanding { get; set; }
    public int DisputeCount { get; set; }
    public int InvoiceCount { get; set; }
}
