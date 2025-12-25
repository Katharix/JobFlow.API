namespace JobFlow.Business.Models.DTOs;

public class SetDefaultPaymentMethodRequest
{
    public Guid ProfileId { get; set; }
    public string PaymentMethodId { get; set; } = string.Empty;
}