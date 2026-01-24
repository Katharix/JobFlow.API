namespace JobFlow.Business.Utilities;

public static class InventoryCalculator
{
    public static decimal CalculateInventoryUsage(decimal quantitySold, decimal inventoryUnitsPerSale)
    {
        if (quantitySold <= 0 || inventoryUnitsPerSale <= 0)
            return 0;

        return quantitySold * inventoryUnitsPerSale;
    }
}