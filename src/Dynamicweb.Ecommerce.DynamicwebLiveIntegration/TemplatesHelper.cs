using Dynamicweb.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Products;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.ProductCatalog;
using Dynamicweb.Ecommerce.Products;
using System;
using System.Collections.Generic;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration
{
    /// <summary>
    /// Helper class to provide live integration information to templates.
    /// </summary>
    public static class TemplatesHelper
    {
        /// <summary>
        /// Indicates if the system is able to connect to the ERP.
        /// </summary>
        [Obsolete("Use Context.Current.Items[\"IsWebServiceConnectionAvailable\"] instead")]
        public static bool IsWebServiceConnectionAvailable()
        {
            return Connector.IsWebServiceConnectionAvailable(SettingsManager.GetSettingsByShop(Global.CurrentShopId), SubmitType.Live);
        }

        /// <summary>
        /// Determines whether the last order sync for the current context failed.
        /// </summary>
        public static bool HasErrorOnExportOrder()
        {
            if (Context.Current != null && Context.Current.Session != null
                && bool.TryParse(Context.Current.Session["DynamicwebLiveIntegration.OrderExportFailed"].ToString(), out bool hasErrorOnExportOrder))
            {
                return hasErrorOnExportOrder;
            }

            return false;
        }

        /// <summary>
        /// Gets the ID of the last failed order.
        /// </summary>
        /// <returns>System.String.</returns>
        public static string FailedOrderId()
        {
            if (Context.Current != null && Context.Current.Session != null)
            {
                var o = Context.Current.Session["DynamicwebLiveIntegration.FailedOrderId"];
                return o?.ToString() ?? string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// Forces an update of the order in the ERP.
        /// </summary>
        /// <param name="orderId">The ID of the order to send to the ERP</param>
        /// <returns>Returns null if no communication has made, otherwise it returns true / false to indicate success or failure in sending the order.</returns>
        public static bool? UpdateOrder(string orderId)
        {
            return UpdateOrder(Services.Orders.GetById(orderId));
        }

        /// <summary>
        /// Forces an update of the order in the ERP.
        /// </summary>
        /// <param name="order">The order to send to the ERP</param>
        /// <returns>Returns null if no communication has made, otherwise it returns true / false to indicate success or failure in sending the order.</returns>
        public static bool? UpdateOrder(Order order)
        {
            return OrderHandler.UpdateOrder(SettingsManager.GetSettingsByShop(order.ShopId), order, SubmitType.FromTemplates);
        }

        /// <summary>
        /// Pick an existing order and updates product information on it
        /// </summary>
        /// <param name="orderId">The ID of the order.</param>
        public static void UpdateStockOnOrder(string orderId)
        {
            UpdateStockOnOrder(Services.Orders.GetById(orderId));
        }

        /// <summary>
        /// Updates product information on the order.
        /// </summary>
        /// <param name="order">The order.</param>
        public static void UpdateStockOnOrder(Order order)
        {
            // call LI method to check stocks in ERP
            NotificationSubscribers.NotificationSubscriberBase.SetProductInformation(SettingsManager.GetSettingsByShop(order.ShopId), order);
        }

        /// <summary>
        /// Updates the specified product with live information from the ERP.
        /// </summary>
        /// <param name="product">The product.</param>
        /// <param name="quantity">The quantity.</param>
        /// <param name="updateCache">Update response cache.</param>
        /// <returns><c>true</c> if product information was updated, <c>false</c> otherwise.</returns>
        public static bool UpdateProduct(Product product, double quantity, string currencyCode, string shopId, bool updateCache = false)
        {
            var settings = SettingsManager.GetSettingsByShop(shopId);
            var productSelection = product.GetPriceProductSelection(1, null);
            var context = new LiveContext(Services.Currencies.GetCurrency(currencyCode), Helpers.GetCurrentExtranetUser(), Services.Shops.GetShop(shopId));
            return Products.ProductManager.FetchProductInfos(
                new List<Prices.PriceProductSelection>() { productSelection },
                context,
                settings, new Logging.Logger(settings), false, SubmitType.Live, updateCache);
        }

        /// <summary>
        /// Determine if the product live information is active and should be loaded in AJAX call.
        /// </summary>
        [Obsolete("Use Context.Current.Items[\"IsLazyLoadingForProductInfoEnabled\"] instead")]
        public static bool IsLazyLoadingForProductInfoEnabled
        {
            get
            {
                var settings = SettingsManager.GetSettingsByShop(Global.CurrentShopId);
                var user = Helpers.GetCurrentExtranetUser();
                return Global.IsIntegrationActive(settings) && settings.EnableLivePrices && Connector.IsWebServiceConnectionAvailable(settings, SubmitType.Live)
                    && (settings.LiveProductInfoForAnonymousUsers || user != null)
                    && (user == null || !user.IsLiveIntegrationPricesDisabled())
                    && settings.LazyLoadProductInfo;
            }
        }

        /// <summary>
        /// Updates the product fields in the specified product view model with live information from the ERP.
        /// </summary>
        /// <param name="product">The product view model.</param>
        public static void UpdateProductInViewModel(ProductViewModel product)
        {
            var shopId = Global.CurrentShopId;
            var settings = SettingsManager.GetSettingsByShop(shopId);
            if (settings == null)
                return;

            var user = Helpers.GetCurrentExtranetUser();
            var loadedProduct = Services.Products.GetProductById(product.Id, product.VariantId, product.LanguageId);
            if (loadedProduct == null || !Helpers.CanCheckPrice(settings, loadedProduct, user))
                return;

            var logger = new Logging.Logger(settings);
            bool showVariantDefault = SystemConfiguration.Instance.GetBoolean("/Globalsettings/Ecom/Product/ShowVariantDefault");
            var productForRequest = showVariantDefault ? ProductManager.ProductProvider.GetProductFromVariantComboId(loadedProduct, logger) : loadedProduct;
            var context = new LiveContext(Helpers.GetCurrentCurrency(), user, null);
            string unitId = settings.UseUnitPrices ? productForRequest.DefaultUnitId : null;
            var productSelection = productForRequest.GetPriceProductSelection(1, unitId);

            var productInfo = ProductManager.GetProductInfo(productForRequest, settings, user);
            if (productInfo == null)
            {
                if (!ProductManager.FetchProductInfos([productSelection], context, settings, logger, false, SubmitType.Live, true))
                    return;

                productInfo = ProductManager.GetProductInfo(productForRequest, settings, user);
            }

            if (productInfo == null)
                return;

            ProductManager.ProductProvider.FillProductValues(productInfo, loadedProduct, settings, productSelection.Quantity, context);
        }
    }
}