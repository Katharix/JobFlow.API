using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFlow.Business.Utilities
{
    public static class InventoryCalculator
    {
        public static decimal CalculateInventoryUsage(decimal quantitySold, decimal inventoryUnitsPerSale)
        {
            if (quantitySold <= 0 || inventoryUnitsPerSale <= 0)
                return 0;

            return quantitySold * inventoryUnitsPerSale;
        }
    }
}
