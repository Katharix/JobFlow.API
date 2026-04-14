namespace JobFlow.Business.ModelErrors;

public static class InvoiceErrors
{
    public static readonly Error NotFound = Error.NotFound("Invoice.NotFound", "The invoice was not found.");
    public static readonly Error InvalidAmount = Error.Validation("Invoice.InvalidAmount", "The refund amount must be greater than zero.");
    public static readonly Error RefundExceedsTotal = Error.Validation("Invoice.RefundExceedsTotal", "The total refunded amount cannot exceed the invoice total.");
}