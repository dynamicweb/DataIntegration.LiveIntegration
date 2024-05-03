using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Products;
using Dynamicweb.Ecommerce.ProductCatalog;
using Dynamicweb.Ecommerce.Products;
using Dynamicweb.Security.UserManagement;
using System.Collections.Generic;
using System.Linq;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration
{
    public static class ProductViewModelExtensions
    {
        /// <summary>
        /// Gets the prices found for different product units, quantities, etc in the ERP
        /// </summary>
        /// <param name="productViewModel">Product ViewModel</param>
        /// <returns>List of prices found for product in the response "EcomPrices" xml tags</returns>
        public static List<PriceListViewModel> GetUnitPrices(this ProductViewModel productViewModel)
        {
            var result = new List<PriceListViewModel>();

            var settings = SettingsManager.GetSettingsByShop(Global.CurrentShopId);
            if (!Global.IsIntegrationActive(settings))
                return result;

            var user = Helpers.GetCurrentExtranetUser();
            var product = Services.Products.GetProductById(productViewModel.Id, productViewModel.VariantId, productViewModel.LanguageId);

            var prices = GetUnitPrices(settings, user, product);
            if (prices is not null && prices.Count > 0)
            {
                foreach (var price in prices.OrderBy(p => p.Quantity.GetValueOrDefault()))
                {
                    result.Add(new()
                    {
                        Quantity = price.Quantity ?? 0,
                        UnitId = price.UnitId,
                        Price = new()
                        {
                            Price = price.Amount.GetValueOrDefault(),
                            PriceWithVat = price.AmountWithVat.GetValueOrDefault(),
                            PriceWithoutVat = price.Amount.GetValueOrDefault(),
                            CurrencyCode = Common.Context.Currency.Code,
                            ShowPricesWithVat = Common.Context.DisplayPricesWithVat
                        }
                    });
                }
            }
            return result;
        }

        internal static List<ProductPrice> GetUnitPrices(Settings settings, User user, Product product)
        {
            if (settings is not null && Helpers.CanCheckPrice(settings, product, user))
            {
                var productInfo = ProductManager.GetProductInfo(product, settings, user);
                if (productInfo is not null)
                {
                    return ((IList<ProductPrice>)productInfo["Prices"]).ToList();
                }
            }
            return new List<ProductPrice>();
        }
    }
}
