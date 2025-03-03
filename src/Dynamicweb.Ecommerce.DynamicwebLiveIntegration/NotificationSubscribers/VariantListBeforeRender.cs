using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Products;
using Dynamicweb.Ecommerce.Products;
using Dynamicweb.Extensibility.Notifications;
using Dynamicweb.Security.UserManagement;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.NotificationSubscribers
{
    [Subscribe(Ecommerce.Notifications.Ecommerce.VariantList.BeforeRender)]
    public class VariantListBeforeRender : NotificationSubscriberBase
    {
        public override void OnNotify(string notification, NotificationArgs args)
        {
            if (args is Ecommerce.Notifications.Ecommerce.VariantList.BeforeRenderArgs beforeRenderArgs)
            {
                var variantCombinationProducts = beforeRenderArgs.VariantCombinationsProducts;
                if (variantCombinationProducts != null) {
                    var settings = SettingsManager.GetSettingsByShop(Global.CurrentShopId);
                    if (settings != null &&
                        settings.AddProductFieldsToRequest &&
                        settings.GetProductInformationForAllVariants &&
                        CanCheckPrice(settings))
                    {
                        var user = UserContext.Current.User;
                        foreach (var vcp in variantCombinationProducts)
                        {
                            var variantProduct = vcp.Value;
                            if (variantProduct == null)
                                continue;
                            var productInfo = ProductManager.GetProductInfo(variantProduct, settings, user);
                            if (productInfo != null)
                            {
                                if (variantProduct.ProductFieldValues == null)
                                {
                                    ProductService service = new ProductService();
                                    service.SetDefaultProductFields(variantProduct);
                                }
                                if (variantProduct.ProductFieldValues != null)
                                {
                                    ProductManager.FillProductFieldValues(variantProduct, productInfo);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool CanCheckPrice(Settings settings)
        {
           return EnabledAndActive(settings, SubmitType.Live) && settings.EnableLivePrices && 
                (settings.LiveProductInfoForAnonymousUsers || Helpers.GetCurrentExtranetUser() != null) && 
                (Helpers.GetCurrentExtranetUser() == null || !Helpers.GetCurrentExtranetUser().IsLivePricesDisabled) && 
                !Global.IsProductLazyLoad(settings); 
        }
    }
}
