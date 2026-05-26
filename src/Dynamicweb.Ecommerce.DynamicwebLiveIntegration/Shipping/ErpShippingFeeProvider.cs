using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Prices;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Shipping
{
    internal class ErpShippingFeeProvider
    {
        private const string CachePrefix = "ErpShippingFeeProvider_";

        internal static string OrderMarkerKey(string orderId) => CachePrefix + orderId;
        internal static string MethodCacheKey(string orderId, string shippingMethodId) => CachePrefix + orderId + "_" + shippingMethodId;        

        internal static void ProcessShipping(Settings settings, Order order, XmlNode orderNode, Logger logger)
        {
            string shippingFee = orderNode.SelectSingleNode("column [@columnName='OrderShippingFee']")?.InnerText;
            if (string.IsNullOrEmpty(shippingFee))
            {
                ClearCache(order);
                return;
            }

            double fee = Helpers.ToDouble(settings, logger, shippingFee);

            string shippingFeeWithoutVat = orderNode.SelectSingleNode("column [@columnName='OrderShippingFeeWithoutVat']")?.InnerText;
            double feeWithoutVat = 0;
            double vatPercent = order.Price.VATPercent;
            if (!string.IsNullOrEmpty(shippingFeeWithoutVat))
            {
                feeWithoutVat = Helpers.ToDouble(settings, logger, shippingFeeWithoutVat);
                feeWithoutVat = feeWithoutVat > fee ? fee : feeWithoutVat;
                vatPercent = feeWithoutVat > 0 ? (fee / feeWithoutVat - 1) * 100 : 0;
            }
            if (feeWithoutVat <= 0)
            {
                feeWithoutVat = MinusVat(fee, vatPercent);
            }

            var price = new PriceInfo(order.Currency);
            price.PriceWithVAT = fee;
            price.PriceWithoutVAT = feeWithoutVat;
            price.VATPercent = vatPercent;
            price.VAT = fee - feeWithoutVat;

            AddToCache(order, price);

            order.AllowOverrideShippingFee = true;
            order.ShippingFee.PriceWithVAT = price.PriceWithVAT;
            order.ShippingFee.VATPercent = price.VATPercent;
            order.ShippingFee.PriceWithoutVAT = price.PriceWithoutVAT;
            order.ShippingFee.VAT = price.VAT;            
        }

        internal static double MinusVat(double price, double percent)
        {
            return (double)((decimal)price / ((decimal)percent / 100M + 1M));
        }

        private static void AddToCache(Order order, PriceInfo shippingFee)
        {
            if (Context.Current?.Session is null || string.IsNullOrEmpty(order.ShippingMethodId))
                return;

            // Existence marker lets the notification subscriber know ERP fee caching is active for this order
            Context.Current.Session[OrderMarkerKey(order.Id)] = true;
            Context.Current.Session[MethodCacheKey(order.Id, order.ShippingMethodId)] = shippingFee;
        }

        private static void ClearCache(Order order)
        {
            if (Context.Current?.Session is null)
                return;

            Context.Current.Session.Remove(OrderMarkerKey(order.Id));
            if (!string.IsNullOrEmpty(order.ShippingMethodId))
                Context.Current.Session.Remove(MethodCacheKey(order.Id, order.ShippingMethodId));
        }
    }
}
