using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Cache;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Products;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Extensibility.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.NotificationSubscribers
{
    /// <summary>
    /// Base notification subscriber for Live Integration, for common functionality.
    /// </summary>
    /// <seealso cref="NotificationSubscriber" />
    public abstract class NotificationSubscriberBase : NotificationSubscriber
    {
        /// <summary>
        /// Gets a value that determines whether live integration is enabled for the current shop, active and a connection to the ERP is available.
        /// </summary>
        /// <returns><c>true</c> if the live integration is enabled and active, <c>false</c> otherwise.</returns>
        [Obsolete("Use EnabledAndActive(Settings settings, SubmitType submitType) instead")]
        protected static bool EnabledAndActive(Settings settings) => EnabledAndActive(settings, SubmitType.Live);
        /// <summary>
        /// Gets a value that determines whether live integration is enabled for the current shop, active and a connection to the ERP is available.
        /// </summary>
        /// <returns><c>true</c> if the live integration is enabled and active, <c>false</c> otherwise.</returns>
        protected static bool EnabledAndActive(Settings settings, SubmitType submitType)
        {
            var cacheValue = Context.Current?.Items?["DynamicwebLiveIntegrationEnabledAndActive"];
            if (cacheValue != null)
            {
                return (bool)cacheValue;
            }
            else
            {
                bool result = Global.IsIntegrationActive(settings)
                                && Connector.IsWebServiceConnectionAvailable(settings, submitType);
                if (Context.Current?.Items != null)
                {
                    Context.Current.Items["DynamicwebLiveIntegrationEnabledAndActive"] = result;
                }
                return result;
            }
        }

        /// <summary>
        /// Clears and sets the product information in Dynamicweb for all the products in the order.
        /// </summary>
        /// <param name="order">The order.</param>
        protected internal static void SetProductInformation(Settings settings, Order order)
        {
            if (order == null || !order.OrderLines.Any(ol => ol.IsProduct()))
            {
                return;
            }

            // clear product cache to ensure refresh from ERP
            ResponseCache.ClearAllCaches();

            // read all products in the order
            var productsToUpdate = GetProductsWithQuantities(order);
            if (productsToUpdate != null && productsToUpdate.Any())
            {
                var logger = new Logger(settings);
                logger.Log(ErrorLevel.DebugInfo, $"Reload prices. products #{productsToUpdate.Count}");

                var context = new LiveContext(order.GetPriceContext());
                if (!ProductManager.FetchProductInfos(productsToUpdate, context, settings, logger, false, SubmitType.Live))
                {
                    return;
                }
                // Set values
                foreach (var productWithQuantity in productsToUpdate)
                {
                    ProductManager.FillProductValues(settings, productWithQuantity.Product, productWithQuantity.Quantity, context, productWithQuantity.UnitId);
                }
            }
        }

        /// <summary>
        /// Gets the products and their current quantities from the order lines.
        /// </summary>
        /// <param name="order">The order from which the products are retrieved.</param>
        private static List<PriceProductSelection> GetProductsWithQuantities(Order order)
        {
            List<PriceProductSelection> products = new List<PriceProductSelection>();
            var productProvider = ProductManager.ProductProvider;
            foreach (var ol in order.OrderLines)
            {
                if (ol.IsProduct())
                {
                    if (!products.Any(p => p.Product == ol.Product))
                        products.Add(ol.Product.GetPriceProductSelection(ol.Quantity, ol.UnitId));
                }
            }
            return products;
        }

        protected bool IsCreateOrderAllowed(Order order)
        {
            if (order.Complete && order.RecurringOrderId != 0)
            {
                // Check if the currenct order is recurring
                var recurringOrder = RecurringOrder.GetRecurringOrderById(order.RecurringOrderId);
                if (recurringOrder != null && recurringOrder.StartDate.HasValue && recurringOrder.StartDate.Value > System.DateTime.Now)
                {
                    //Canceling submission to ERP because order is in the future                        
                    return false;
                }
            }
            return true;
        }
    }
}