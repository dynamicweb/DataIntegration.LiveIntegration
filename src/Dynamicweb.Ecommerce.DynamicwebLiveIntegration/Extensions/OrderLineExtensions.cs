using System;
using Dynamicweb.Ecommerce.Orders;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions
{
    /// <summary>
    /// Extension methods for order lines.
    /// </summary>
    public static class OrderLineExtensions
    {
        /// <summary>
        /// Determines if this order line represents a product (and not a discount or tax line for example).
        /// </summary>
        /// <param name="orderLine">The order line.</param>
        /// <returns>Returns <c>true</c> if the specified order line is a product; otherwise, <c>false</c>.</returns>
        public static bool IsProduct(this OrderLine orderLine)
        {
            return orderLine.OrderLineType == OrderLineType.Product || orderLine.OrderLineType == OrderLineType.Fixed;
        }

        /// <summary>
        /// Determines if this order line represents a discount (and not a product or tax line for example).
        /// </summary>
        /// <param name="orderLine">The order line.</param>
        /// <returns>Returns <c>true</c> if the specified order line is a discount; otherwise, <c>false</c>.</returns>
        public static bool IsDiscount(this OrderLine orderLine)
        {
            return orderLine.OrderLineType == OrderLineType.Discount || orderLine.OrderLineType == OrderLineType.ProductDiscount;
        }

        /// <summary>
        /// Gets the order line type description.
        /// </summary>
        /// <param name="orderLine">The order line.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="NotSupportedException"></exception>
        public static string GetOrderLineTypeDescription(this OrderLine orderLine)
        {
            if (orderLine.IsProduct())
            {
                return "Product";
            }

            if (orderLine.IsDiscount())
            {
                return "Discount";
            }

            if (orderLine.OrderLineType == OrderLineType.Tax)
            {
                return "Tax";
            }

            throw new NotSupportedException($"Unsupported type {orderLine.OrderLineType}");
        }

        /// <summary>
        /// Clones the order line.
        /// </summary>
        /// <param name="orderLine">The order line.</param>
        /// <returns>OrderLine.</returns>
        public static OrderLine CloneOrderLine(this OrderLine orderLine)
        {
            OrderLine newOrderLine = new OrderLine(orderLine.Order)
            {
                Quantity = orderLine.Quantity,
                OrderLineType = orderLine.OrderLineType,
                ProductName = orderLine.ProductName,
                ParentLineId = orderLine.ParentLineId,
                UnitPrice =
                {
                    PriceWithVAT = orderLine.UnitPrice.PriceWithVAT,
                    PriceWithoutVAT = orderLine.UnitPrice.PriceWithoutVAT,
                    VAT = orderLine.UnitPrice.VAT
                }
            };

            newOrderLine.Price.PriceWithVAT = orderLine.Price.PriceWithVAT;
            newOrderLine.Price.PriceWithoutVAT = orderLine.Price.PriceWithoutVAT;
            newOrderLine.DiscountId = orderLine.DiscountId;

            return newOrderLine;
        }
    }
}