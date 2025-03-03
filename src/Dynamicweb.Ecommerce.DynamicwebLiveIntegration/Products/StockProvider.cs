using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Ecommerce.Products;
using Dynamicweb.Ecommerce.Stocks;
using System.Collections.Generic;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Products
{
    public class StockProvider : StockLevelProvider
    {
        public override double? FindStockLevel(Product product, string unitId, StockLocation stockLocation)
        {
            var user = Helpers.GetCurrentExtranetUser();
            var settings = SettingsManager.GetSettingsByShop(Global.CurrentShopId);
            if (Helpers.CanCheckPrice(settings, product, user))
            {
                Diagnostics.ExecutionTable.Current.Add($"DynamicwebLiveIntegration.StockProvider.FindStockLevel product[id='{product?.Id}' variantId='{product.VariantId}'] START");
                var priceProductSelection = product.GetPriceProductSelection(1, unitId);
                var products = new List<PriceProductSelection>() { priceProductSelection };

                var context = new LiveContext(Helpers.GetCurrentCurrency(), user, Services.Shops.GetShop(Global.CurrentShopId));

                var logger = new Logger(settings);
                if (ProductManager.FetchProductInfos(products, context, settings, logger, false, SubmitType.Live))
                {
                    ProductInfo productInfo = ProductManager.GetProductInfo(product, settings, user, context, priceProductSelection.UnitId);
                    if (productInfo != null)
                    {
                        if (settings?.AddProductFieldsToRequest ?? false)
                        {
                            ProductManager.FillProductFieldValues(product, productInfo);
                        }
                        return (double?)productInfo["Stock"];
                    }
                }
                Diagnostics.ExecutionTable.Current.Add($"DynamicwebLiveIntegration.StockProvider.FindStockLevel product[id='{product?.Id}' variantId='{product.VariantId}'] END");
            }
            return null;
        }
    }
}
