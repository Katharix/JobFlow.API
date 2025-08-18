
namespace JobFlow.Business.ModelErrors
{
    public static partial class PriceBookCategoryErrors
    {
        public static Error PriceBookCategoryNotFound => Error.NotFound(
         "PriceBookCategory", "Price book category not found.");

        public static Error CategoryDuplicateName => Error.Conflict(
            "PriceBookCategory", "A price book category with this name already exists.");

        public static Error PriceBookCategoryNameExists => Error.Conflict(
            "PriceBookCategory", "A category with this name already exists for this organization.");
    }
}
