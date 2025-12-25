namespace JobFlow.Business.ModelErrors;

public static class InvoiceErrors
{
    public static readonly Error NotFound = Error.NotFound("Invoice.NotFound", "The invoice was not found.");
}