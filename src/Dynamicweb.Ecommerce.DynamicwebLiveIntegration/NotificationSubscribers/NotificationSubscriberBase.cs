using Dynamicweb.Content;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Cache;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Products;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Products;
using Dynamicweb.Extensibility.Notifications;
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
        protected static bool EnabledAndActive(Settings settings)
        {
            var cacheValue = Context.Current?.Items?["DynamicwebLiveIntegrationEnabledAndActive"];
            if (cacheValue != null)
            {
                return (bool)cacheValue;
            }
            else
            {
                bool result = Global.IsIntegrationActive(settings)                                
                                && Connector.IsWebServiceConnectionAvailable(settings);
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
                logger.Log(ErrorLevel.DebugInfo, $"Reload prices. products #{productsToUpdate.Count()}");
                
                var context = new LiveContext(order.GetPriceContext());
                if (!ProductManager.FetchProductInfos(productsToUpdate, context, settings, logger, false))
                {
                    return;
                }
                // Set values
                foreach (var productWithQuantity in productsToUpdate)
                {
                    ProductManager.FillProductValues(settings, productWithQuantity.Key, productWithQuantity.Value, context);
                }
            }
        }

        /// <summary>
        /// Gets the products and their current quantities from the order lines.
        /// </summary>
        /// <param name="order">The order from which the products are retrieved.</param>
        private static Dictionary<Product, double> GetProductsWithQuantities(Order order)
        {
            Dictionary<Product, double> products = new Dictionary<Product, double>();
            var productProvider = ProductManager.ProductProvider;
            foreach (var ol in order.OrderLines)
            {
                if (ol.IsProduct())
                {
                    var product = productProvider.GetProductWithUnit(ol.Product, ol.UnitId);
                    if (!products.ContainsKey(ol.Product))
                        products.Add(product, ol.Quantity);
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