namespace JobFlow.Business.ModelErrors;

public static class PriceBookErrors
{
    public static Error PriceBookItemNotFound => Error.NotFound("PriceBook", "Price book item not found.");
    public static Error InvalidPrice => Error.Failure("PriceBook", "Price must be greater than zero.");

    public static Error DuplicateName =>
        Error.Conflict("PriceBook", "A price book item with this name already exists.");
}