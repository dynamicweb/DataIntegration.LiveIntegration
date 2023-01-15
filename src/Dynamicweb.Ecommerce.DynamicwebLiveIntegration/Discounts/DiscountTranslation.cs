using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.Orders;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Discounts
{
    internal class DiscountTranslation
    {
        /// <summary>
        /// Gets the discount name
        /// </summary>
        /// <param name="orderLineNode">order line node</param>
        /// <param name="orderLine">order line</param>
        /// <returns>Translated discount name</returns>
        internal static string GetDiscountName(Settings settings, XmlNode orderLineNode, OrderLine orderLine)
        {
            string discountName = orderLineNode?.SelectSingleNode("column [@columnName='OrderLineProductName']")?.InnerText;
            string translatedDiscountName = null;
            if (orderLine.OrderLineType == OrderLineType.ProductDiscount)
            {
                translatedDiscountName = Helpers.GetTranslation(Constants.OrderConfiguration.ProductDiscountText);
                discountName = string.IsNullOrEmpty(translatedDiscountName) ? settings.ProductDiscountText : translatedDiscountName;
            }
            else if (orderLine.OrderLineType == OrderLineType.Discount)
            {
                translatedDiscountName = Helpers.GetTranslation(Constants.OrderConfiguration.OrderDiscountText);
                discountName = string.IsNullOrEmpty(translatedDiscountName) ? settings.OrderDiscountText : translatedDiscountName;
            }
            return discountName;
        }
    }
}
