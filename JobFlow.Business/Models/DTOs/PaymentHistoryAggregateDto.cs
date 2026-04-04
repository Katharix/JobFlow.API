namespace JobFlow.Business.Models.DTOs;

public class PaymentHistoryAggregateDto
{
    public long GrossCollectedMinor { get; set; }
    public long RefundedMinorAbsolute { get; set; }
    public long MonthCollectedMinor { get; set; }
    public int DisputeCount { get; set; }
}
