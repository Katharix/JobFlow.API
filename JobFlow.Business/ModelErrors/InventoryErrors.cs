namespace JobFlow.Business.ModelErrors
{
    public static class InventoryErrors
    {
        public static Error InventoryItemNotFound => Error.NotFound(
            "Inventory", "Inventory item not found.");

        public static Error InvalidQuantity => Error.Failure(
            "Inventory", "Quantity must be zero or greater.");

        public static Error DuplicateName => Error.Conflict(
            "Inventory", "An inventory item with this name already exists.");
    }
}

