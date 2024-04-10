using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Ecommerce.ProductCatalog;
using System.Collections.Generic;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Products
{

    public class LiveProductViewModel : ProductViewModel, ICustomQuantityPrices
    {
        private object _instance;
        public LiveProductViewModel()
        {
        }

        IEnumerable<PriceListViewModel> ICustomQuantityPrices.GetPrices(PriceContext context)
        {
            var settings = SettingsManager.GetSettingsByShop(context.Shop?.Id);
            if (!Global.IsIntegrationActive(settings))
                return new List<PriceListViewModel>();

            return ProductViewModelExtensions.GetUnitPrices(settings, context.Customer, Services.Products.GetProductById(Id, VariantId, LanguageId));
        }
    }
}
