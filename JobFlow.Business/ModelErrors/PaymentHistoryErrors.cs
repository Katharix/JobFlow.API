namespace JobFlow.Business.ModelErrors;

public static class PaymentHistoryErrors
{
    public static readonly Error NotFound =
        Error.NotFound("PaymentHistory.NotFound", "The payment record was not found.");
}