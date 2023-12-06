using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
using Dynamicweb.Ecommerce.Orders;
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
            return Connector.IsWebServiceConnectionAvailable(SettingsManager.GetSettingsByShop(Global.CurrentShopId));
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
            return Products.ProductManager.FetchProductInfos(
                new Dictionary<Product, double> { { product, quantity } },
                new LiveContext(Services.Currencies.GetCurrency(currencyCode), Helpers.GetCurrentExtranetUser(), Services.Shops.GetShop(shopId)),
                settings, new Logging.Logger(settings), false, updateCache);
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
                return Global.IsIntegrationActive(settings) && settings.EnableLivePrices && Connector.IsWebServiceConnectionAvailable(settings)
                    && (settings.LiveProductInfoForAnonymousUsers || Helpers.GetCurrentExtranetUser() != null)
                    && (Helpers.GetCurrentExtranetUser() == null || !Helpers.GetCurrentExtranetUser().IsLivePricesDisabled)
                    && settings.LazyLoadProductInfo;
            }
        }        
    }
}