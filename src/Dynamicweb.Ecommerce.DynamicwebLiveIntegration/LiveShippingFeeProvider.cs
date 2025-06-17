using Dynamicweb.Ecommerce.Cart;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Extensibility;
using Dynamicweb.Extensibility.AddIns;
using System;
using System.Linq;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration
{
    /// <summary>
    /// Shipping Fee Provider to handle shipping fees coming from the ERP.
    /// </summary>
    /// <seealso cref="ShippingProvider" />
    [AddInName("Live integration shipping fee provider"), AddInDescription("Applies the shipping fee via live integration")]
    public class LiveShippingFeeProvider : ShippingProvider
    {
        /// <summary>
        /// Calculate shipping fee for the specified order.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <returns>Returns shipping fee for the specified order</returns>
        public override PriceRaw CalculateShippingFee(Order order)
        {
            PriceRaw rate = null;

            if (Context.Current != null && Context.Current.Items != null && Context.Current.Items["DynamicwebLiveShippingFeeProvider" + order.Id] != null)
            {                
                double shippingFee = (double)Context.Current.Items["DynamicwebLiveShippingFeeProvider" + order.Id];
                rate = new PriceRaw(shippingFee, order.Currency);
            }            

            return rate;
        }        

        /// <summary>
        /// Processes the shipping.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="orderNode">The order node.</param>
        internal static void ProcessShipping(Settings settings, Order order, XmlNode orderNode, Logger logger)
        {
            if (settings.ErpControlsShipping)
            {
                Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.LiveShippingFeeProvider.ProcessShipping START");
                ProcessLiveIntegrationShipping(settings, order, orderNode, logger);
                Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.LiveShippingFeeProvider.ProcessShipping END");
            }
        }

        /// <summary>
        /// Adds to cache.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="shippingFee">The shipping fee.</param>
        private static void AddToCache(Order order, double shippingFee)
        {
            if (Context.Current != null && Context.Current.Items != null)
            {
                Context.Current.Items["DynamicwebLiveShippingFeeProvider" + order.Id] = shippingFee;
            }
        }

        /// <summary>
        /// Gets the shipping.
        /// </summary>
        /// <returns>Shipping.</returns>
        private static Shipping GetShipping()
        {            
            return Services.Shippings.GetShippingsWithoutRegions(false).FirstOrDefault(s => !string.IsNullOrEmpty(s.ServiceSystemName) && 
                (string.Equals(typeof(LiveShippingFeeProvider).GetTypeNameWithAssembly(), s.ServiceSystemName) ||
                string.Equals(s.ServiceSystemName, typeof(LiveShippingFeeProvider).FullName, StringComparison.OrdinalIgnoreCase)));
        }

        private static void ProcessLiveIntegrationShipping(Settings settings, Order order, XmlNode orderNode, Logger logger)
        {
            string shippingFee = orderNode.SelectSingleNode("column [@columnName='OrderShippingFee']")?.InnerText;
            if (!string.IsNullOrEmpty(shippingFee))
            {
                Shipping liveIntegrationShipping = GetShipping();
                if (liveIntegrationShipping != null)
                {
                    order.ShippingMethodId = liveIntegrationShipping.Id;
                    string shippingName = orderNode.SelectSingleNode("column [@columnName='OrderShippingMethodName']")?.InnerText;

                    if (string.IsNullOrEmpty(shippingName))
                    {
                        shippingName = orderNode.SelectSingleNode("column [@columnName='OrderShippingMethodId']")?.InnerText;
                    }

                    order.ShippingMethod = !string.IsNullOrEmpty(shippingName) ? shippingName : liveIntegrationShipping.GetName(order.LanguageId);

                    double fee = Helpers.ToDouble(settings, logger, shippingFee);
                    AddToCache(order, fee);

                    if (!order.IsCart)
                    {
                        order.ShippingFee.PriceWithVAT = fee;                        
                    }
                    else
                    {
                        // Trigger ShippingProvider to calculate and store shipping value on order
                        var calculateShippingUsingShippingProvider = GetShippingFee(order);
                    }
                }
            }
        }

        internal static PriceInfo GetShippingFee(Order order)
        {
            // Trigger ShippingProvider to calculate and store shipping value on order
            var isPriceCalculatedByProvider = order.IsPriceCalculatedByProvider;
            if (isPriceCalculatedByProvider)
            {
                order.IsPriceCalculatedByProvider = false;
            }
            var calculateShippingUsingShippingProvider = order.ShippingFee;
            if (isPriceCalculatedByProvider)
            {
                order.IsPriceCalculatedByProvider = true;
            }
            return calculateShippingUsingShippingProvider;
        }
    }
}