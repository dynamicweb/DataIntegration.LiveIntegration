using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Ecommerce.Products;
using System.Linq;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions
{
    public static class ProductExtensions
    {
        public static bool IsVariantProductFromFamily(this Product product)
        {
            if (!string.IsNullOrEmpty(product.VariantId))
            {
                return Services.VariantGroups.GetVariantGroupsByProductId(product.Id)?.FirstOrDefault(vg => vg.Family) != null;
            }
            return false;
        }

        public static bool HasIdentifier(this Product product, Settings settings)
        {
            return !settings.CalculatePriceUsingProductNumber || !string.IsNullOrEmpty(product.Number);
        }

        public static bool IsUnitUpdateNeeded(this Product product, string unitId)
        {            
            if (string.IsNullOrEmpty(unitId) || product.DefaultUnitId == unitId ||                
                !new Stocks.UnitOfMeasureService().GetUnitOfMeasures(product.Id).Any(u => u.UnitId.Equals(unitId)))            
                return false;            
            return true;
        }

        public static PriceProductSelection GetPriceProductSelection(this Product product, double quantity, string unitId) => new PriceProductSelection(product, unitId, 0, quantity, 0);
    }
}
