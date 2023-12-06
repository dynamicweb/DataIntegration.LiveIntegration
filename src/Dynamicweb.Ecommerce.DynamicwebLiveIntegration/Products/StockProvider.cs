using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.International;
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
                var products = new Dictionary<Product, double>();
                products.Add(product, 1);

                var logger = new Logger(settings);
                if (ProductManager.FetchProductInfos(products, new LiveContext(null, user, Services.Shops.GetShop(Global.CurrentShopId)), settings, logger, false))
                {
                    ProductInfo productInfo = ProductManager.GetProductInfo(product, settings, user);
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
