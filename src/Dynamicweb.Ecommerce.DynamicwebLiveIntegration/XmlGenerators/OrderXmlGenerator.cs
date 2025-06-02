using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Products;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Extensibility.Notifications;
using Dynamicweb.Security.UserManagement;
using System.Linq;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators
{
    /// <summary>
    /// Generates XML for orders.
    /// </summary>
    /// <seealso cref="XmlGenerator" />
    public class OrderXmlGenerator : XmlGenerator
    {
        /// <summary>
        /// Generates the order XML.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>System.String.</returns>
        public string GenerateOrderXml(Settings currentSettings, Order order, OrderXmlGeneratorSettings settings, Logger logger)
        {
            if (!settings.GenerateXmlForHash)
            {
                NotificationManager.Notify(Notifications.Order.OnBeforeGenerateOrderXml, new Notifications.Order.OnBeforeGenerateOrderXmlArgs(order, settings, currentSettings, logger));
            }
            var xmlDocument = BuildXmlDocument();
            var tablesNode = CreateAndAppendTablesNode(xmlDocument, settings);
            tablesNode.AppendChild(BuildOrderXml(currentSettings, xmlDocument, order, settings, logger));
            tablesNode.AppendChild(BuildOrderLinesXml(currentSettings, xmlDocument, order, settings, logger));
            if (settings.AddOrderLineFieldsToRequest)
            {
                tablesNode.AppendChild(BuildOrderLineFieldsXml(xmlDocument, order));
            }

            if (!settings.GenerateXmlForHash)
            {
                NotificationManager.Notify(Notifications.Order.OnAfterGenerateOrderXml, new Notifications.Order.OnAfterGenerateOrderXmlArgs(order, settings, xmlDocument, currentSettings, logger));
            }

            if (settings.Beautify)
            {
                return xmlDocument.Beautify();
            }

            return xmlDocument.InnerXml;
        }

        /// <summary>
        /// Adds the customer information to the XML node.
        /// </summary>
        /// <param name="orderNode">The order node.</param>
        /// <param name="order">The order.</param>
        private static void AddCustomerInformation(Settings currentSettings, XmlElement orderNode, Order order, User user)
        {            
            AddChildXmlNode(orderNode, "OrderCustomerAccessUserExternalId", !string.IsNullOrWhiteSpace(user?.ExternalID) ? user.ExternalID : currentSettings.AnonymousUserKey);
            AddChildXmlNode(orderNode, "OrderCustomerNumber", !string.IsNullOrWhiteSpace(user?.CustomerNumber) ? user.CustomerNumber : currentSettings.AnonymousUserKey);
            AddChildXmlNode(orderNode, "OrderCustomerName", !string.IsNullOrWhiteSpace(user?.Name) ? user.Name : order.CustomerName);
            AddChildXmlNode(orderNode, "OrderCustomerAddress", order.CustomerAddress);
            AddChildXmlNode(orderNode, "OrderCustomerAddress2", order.CustomerAddress2);
            AddChildXmlNode(orderNode, "OrderCustomerCity", order.CustomerCity);
            AddChildXmlNode(orderNode, "OrderCustomerState", order.CustomerRegion);
            AddChildXmlNode(orderNode, "OrderCustomerZip", order.CustomerZip);
            AddChildXmlNode(orderNode, "OrderCustomerCountryCode", order.CustomerCountryCode);
            AddChildXmlNode(orderNode, "OrderCustomerEmail", order.CustomerEmail);
            AddChildXmlNode(orderNode, "OrderCustomerPhone", order.CustomerPhone);
            AddChildXmlNode(orderNode, "OrderCustomerFax", order.CustomerFax);
            AddChildXmlNode(orderNode, "OrderCustomerCompany", order.CustomerCompany);
            AddChildXmlNode(orderNode, "OrderCustomerComment", order.CustomerComment);
            AddChildXmlNode(orderNode, "OrderCustomerFirstName", order.CustomerFirstName);
            AddChildXmlNode(orderNode, "OrderCustomerSurname", order.CustomerSurname);
            AddChildXmlNode(orderNode, "OrderCustomerRefId", order.CustomerRefId);
        }

        /// <summary>
        /// Adds the order delivery information to the XML node.
        /// </summary>
        /// <param name="orderNode">The order node.</param>
        /// <param name="order">The order.</param>
        private void AddOrderDeliveryInformation(OrderXmlGeneratorSettings settings, XmlElement orderNode, Order order, User user)
        {
            UserAddress deliveryAddress = null;
            if (settings.CreateOrder && order.DeliveryAddressId > 0)
            {
                deliveryAddress = UserManagementServices.UserAddresses.GetAddressById(order.DeliveryAddressId);                
            }
            string deliveryName = !string.IsNullOrEmpty(order.DeliveryName) ? order.DeliveryName :
                !string.IsNullOrWhiteSpace(order.CustomerName) ? order.CustomerName : user?.Name ?? "";                

            AddChildXmlNode(orderNode, "OrderDeliveryName", deliveryName);
            AddChildXmlNode(orderNode, "OrderDeliveryAddress", order.DeliveryAddress);
            AddChildXmlNode(orderNode, "OrderDeliveryAddress2", order.DeliveryAddress2);
            AddChildXmlNode(orderNode, "OrderDeliveryCity", order.DeliveryCity);
            AddChildXmlNode(orderNode, "OrderDeliveryState", order.DeliveryRegion);
            AddChildXmlNode(orderNode, "OrderDeliveryZip", order.DeliveryZip);
            AddChildXmlNode(orderNode, "OrderDeliveryCountryCode", order.DeliveryCountryCode);
            AddChildXmlNode(orderNode, "OrderDeliveryEmail", order.DeliveryEmail);
            AddChildXmlNode(orderNode, "OrderDeliveryPhone", order.DeliveryPhone);
            AddChildXmlNode(orderNode, "OrderDeliveryFax", order.DeliveryFax);
            AddChildXmlNode(orderNode, "OrderDeliveryCompany", order.DeliveryCompany);
            if (deliveryAddress is object)
            {
                AddChildXmlNode(orderNode, "OrderDeliveryAddressId", !string.IsNullOrEmpty(deliveryAddress.ExternalID) ? deliveryAddress.ExternalID : deliveryAddress.UniqueIdentifier);
            }
        }

        /// <summary>
        /// Appends the order fields to the XML node.
        /// </summary>
        /// <param name="orderNode">The order node.</param>
        /// <param name="order">The order.</param>
        private void AppendOrderFields(Settings settings, XmlElement orderNode, Order order, Logger logger)
        {
            foreach (var field in order.OrderFieldValues)
            {
                string value = field.Value?.ToIntegrationString(settings, logger) ?? string.Empty;
                AddChildXmlNode(orderNode, field.OrderField.SystemName, value, isCustomField: true);
            }
        }

        /// <summary>
        /// Builds the order line fields XML.
        /// </summary>
        /// <param name="xmlDocument">The XML document.</param>
        /// <param name="order">The order.</param>
        /// <returns>XmlNode.</returns>
        private XmlNode BuildOrderLineFieldsXml(XmlDocument xmlDocument, Order order)
        {
            var orderLineFields = CreateTableNode(xmlDocument, "EcomOrderLineFields");
            var orderLines = order.OrderLines.Where(x => x.OrderLineFieldValues.Any()).ToList();
            foreach (var orderLine in orderLines)
            {
                foreach (var field in orderLine.OrderLineFieldValues)
                {
                    var itemNode = CreateAndAppendItemNode(orderLineFields, "EcomOrderLineFields");
                    AddChildXmlNode(itemNode, "OrderLineFieldOrderLineId", orderLine.Id);
                    AddChildXmlNode(itemNode, "OrderLineFieldName", field.OrderLineFieldName ?? string.Empty);
                    AddChildXmlNode(itemNode, "OrderLineFieldValue", field.Value ?? string.Empty);
                    AddChildXmlNode(itemNode, "OrderLineFieldSystemName", field.OrderLineFieldSystemName ?? string.Empty);
                }
            }

            return orderLineFields;
        }

        /// <summary>
        /// Builds the order lines XML.
        /// </summary>
        /// <param name="xmlDocument">The XML document.</param>
        /// <param name="order">The order.</param>
        /// <param name="settings">Settings for generating orderline xml</param>
        /// <returns>Xml generated for an orderline</returns>
        private XmlNode BuildOrderLinesXml(Settings currentSettings, XmlDocument xmlDocument, Order order, OrderXmlGeneratorSettings settings, Logger logger)
        {
            var tableNode = CreateTableNode(xmlDocument, "EcomOrderLines");

            // Order lines (products, taxes)
            var orderLines = order.OrderLines.Where(ol => !ol.IsDiscount()).ToList();
            foreach (var orderLine in orderLines)
            {
                CreateOrderLineXml(currentSettings, tableNode, orderLine, settings, logger);
            }

            if (!settings.GenerateXmlForHash || !settings.ErpControlsDiscount)
            {
                // Order lines (order discounts, and product discounts)                
                orderLines = order.OrderLines.Where(ol => ol.IsDiscount()).ToList();
                foreach (var orderLine in orderLines)
                {
                    if (!string.IsNullOrEmpty(orderLine.GiftCardCode))
                    {
                        if (_orderPriceWithoutVat is null)
                        {
                            _orderPriceWithoutVat = GetOrderPriceWithoutVat(order);
                        }
                        CreateOrderLineXml(currentSettings, tableNode, orderLine, settings, logger);
                    }
                    else
                    {
                        CreateOrderLineXml(currentSettings, tableNode, orderLine, settings, logger);
                    }
                }
            }

            return tableNode;
        }

        /// <summary>
        /// Builds the order XML.
        /// </summary>
        /// <param name="xmlDocument">The XML document.</param>
        /// <param name="order">The order.</param>
        /// <param name="settings">The settings.</param>
        /// <returns>XmlNode.</returns>
        private XmlNode BuildOrderXml(Settings currentSettings, XmlDocument xmlDocument, Order order, OrderXmlGeneratorSettings settings, Logger logger)
        {
            var tableNode = CreateTableNode(xmlDocument, "EcomOrders");
            var itemNode = CreateAndAppendItemNode(tableNode, "EcomOrders");
            var user = UserManagementServices.Users.GetUserById(order.CustomerAccessUserId);

            AddCustomerInformation(currentSettings, itemNode, order, user);
            AddOrderDeliveryInformation(settings, itemNode, order, user);

            // do not use order.Modified in XML unless the field can be ignored for hash calculation
            AddChildXmlNode(itemNode, "CreateOrder", settings.CreateOrder.ToString());
            AddChildXmlNode(itemNode, "OrderId", order.Id);
            AddChildXmlNode(itemNode, "OrderAutoId", order.AutoId.ToString());
            AddChildXmlNode(itemNode, "OrderIntegrationOrderId", order.IntegrationOrderId);
            AddChildXmlNode(itemNode, "OrderCurrencyCode", order.CurrencyCode);
            AddChildXmlNode(itemNode, "OrderDate", order.Date.ToIntegrationString());
            if (!settings.GenerateXmlForHash)
            {
                AddChildXmlNode(itemNode, "OrderPaymentMethodName", order.PaymentMethod, true);
                AddChildXmlNode(itemNode, "OrderPaymentMethodId", order.PaymentMethodId);
                AddChildXmlNode(itemNode, "OrderPaymentMethodCode", order.PaymentMethodCode);
                AddChildXmlNode(itemNode, "OrderPaymentMethodTermsCode", order.PaymentMethodTermsCode);
            }
            AddChildXmlNode(itemNode, "OrderShippingDate", order.ShippingDate.HasValue ? order.ShippingDate.ToIntegrationString(currentSettings, logger) : string.Empty);
            AddChildXmlNode(itemNode, "OrderReference", order.Reference);

            if (settings.ErpControlsShipping)
            {
                // AX handles shipping
                AddChildXmlNode(itemNode, "OrderShippingMethodName", string.Empty, true);
                AddChildXmlNode(itemNode, "OrderShippingMethodId", string.Empty);
                AddChildXmlNode(itemNode, "OrderShippingFee", string.Empty);
            }
            else
            {
                // Dynamicweb handles shipping
                AddChildXmlNode(itemNode, "OrderShippingMethodName", order.ShippingMethod, true);
                AddChildXmlNode(itemNode, "OrderShippingMethodId", order.ShippingMethodId);
                if (!settings.GenerateXmlForHash)
                {
                    AddChildXmlNode(itemNode, "OrderShippingFee", order.ShippingFee.PriceWithVAT.ToIntegrationString(currentSettings, logger));
                    AddChildXmlNode(itemNode, "OrderShippingFeeWithoutVat", order.ShippingFee.PriceWithoutVAT.ToIntegrationString(currentSettings, logger));
                }
                AddChildXmlNode(itemNode, "OrderShippingItemType", settings.ErpShippingItemType);
                AddChildXmlNode(itemNode, "OrderShippingItemKey", settings.ErpShippingItemKey);
                AddChildXmlNode(itemNode, "OrderShippingCode", order.ShippingMethodCode);
                AddChildXmlNode(itemNode, "OrderShippingAgentCode", order.ShippingMethodAgentCode);
                AddChildXmlNode(itemNode, "OrderShippingAgentServiceCode", order.ShippingMethodAgentServiceCode);
            }

            if (!settings.GenerateXmlForHash)
            {
                AddChildXmlNode(itemNode, "OrderPriceTotal", order.Price?.PriceWithVAT.ToIntegrationString(currentSettings, logger));
            }
            AddChildXmlNode(itemNode, "OrderCaptureAmount", order.CaptureAmount.ToIntegrationString(currentSettings, logger));
            AddChildXmlNode(itemNode, "OrderVoucherCode", order.VoucherCode);
            AddChildXmlNode(itemNode, "OrderTransactionId", order.TransactionNumber);
            AddChildXmlNode(itemNode, "OrderStateId", order.StateId);
            AddChildXmlNode(itemNode, "OrderStateName", order.OrderState.GetName(Services.Languages.GetDefaultLanguageId()), true);
            AddChildXmlNode(itemNode, "ErpControlsDiscount", settings.ErpControlsDiscount.ToString());
            AddChildXmlNode(itemNode, "VatCountryCode", order.VatCountry?.Code2);
            AddChildXmlNode(itemNode, "VatPostingGroup", order.VatCountry?.VatPostingGroup);

            if (settings.AddOrderFieldsToRequest)
            {
                AppendOrderFields(currentSettings, itemNode, order, logger);
            }

            return tableNode;
        }

        /// <summary>
        /// Creates the order line XML.
        /// </summary>
        /// <param name="orderLinesNode">The order lines node.</param>
        /// <param name="orderline">The orderline.</param>
        /// <param name="settings">Settings for generating the orderline xml.</param>
        private void CreateOrderLineXml(Settings currentSettings, XmlNode orderLinesNode, OrderLine orderline, OrderXmlGeneratorSettings settings, Logger logger)
        {
            if (!settings.GenerateXmlForHash)
            {
                NotificationManager.Notify(Notifications.OrderLine.OnBeforeGenerateOrderLineXml,
                new Notifications.OrderLine.OnBeforeGenerateOrderLineXmlArgs(orderline, settings, currentSettings, logger));
            }
            else if (currentSettings.ErpControlsDiscount && orderline.IsDiscount())
            {
                return;
            }
            if (orderline.Bom && !currentSettings.AddOrderLinePartsToRequest)
            {
                return;
            }

            XmlElement itemNode = CreateAndAppendItemNode(orderLinesNode, "EcomOrderLines");

            // do not use ol.Date in XML unless the field can be ignored for hash calculation
            // do not use ol.Modified in XML unless the field can be ignored for hash calculation
            if (!settings.GenerateXmlForHash)
            {
                AddChildXmlNode(itemNode, "OrderLineId", orderline.Id);
            }
            AddChildXmlNode(itemNode, "OrderLineOrderId", orderline.OrderId);
            AddChildXmlNode(itemNode, "OrderLineParentLineId", orderline.ParentLineId);
            AddChildXmlNode(itemNode, "OrderLineProductId",
                settings.CalculateOrderUsingProductNumber ? orderline.ProductNumber : orderline.ProductId);
            AddChildXmlNode(itemNode, "OrderLineProductVariantId", orderline.ProductVariantId);
            AddChildXmlNode(itemNode, "OrderLineProductNumber", orderline.ProductNumber);
            AddChildXmlNode(itemNode, "OrderLineProductName", orderline.ProductName); // Product name or discount description
            AddChildXmlNode(itemNode, "OrderLineProductIdentifier", ProductManager.ProductProvider.GetProductIdentifier(currentSettings, orderline.Product, orderline.UnitId));
            AddChildXmlNode(itemNode, "OrderLineQuantity", orderline.Quantity.ToIntegrationString(currentSettings, logger));
            AddChildXmlNode(itemNode, "OrderLineUnitId", orderline.UnitId ?? string.Empty);
            if (!settings.GenerateXmlForHash)
            {
                if (!string.IsNullOrEmpty(orderline.GiftCardCode))
                {
                    var priceWithoutVat = GetGiftCardAmountWithoutVat(orderline);
                    AddChildXmlNode(itemNode, "OrderLinePriceWithoutVat", priceWithoutVat.ToIntegrationString(currentSettings, logger));
                    AddChildXmlNode(itemNode, "OrderLineUnitPriceWithoutVat", priceWithoutVat.ToIntegrationString(currentSettings, logger));
                }
                else
                {
                    AddChildXmlNode(itemNode, "OrderLinePriceWithoutVat", orderline.Price.PriceWithoutVAT.ToIntegrationString(currentSettings, logger));
                    AddChildXmlNode(itemNode, "OrderLineUnitPriceWithoutVat", orderline.UnitPrice.PriceWithoutVAT.ToIntegrationString(currentSettings, logger));
                    AddChildXmlNode(itemNode, "NetPrice", orderline.IsProduct() ? orderline.Product.DefaultPrice.ToIntegrationString(currentSettings, logger) : string.Empty);
                }
            }
            AddChildXmlNode(itemNode, "OrderLineType", ((int)orderline.OrderLineType).ToString());
            AddChildXmlNode(itemNode, "OrderLineTypeName", orderline.OrderLineType.ToString());
            AddChildXmlNode(itemNode, "OrderLineBom", orderline.Bom.ToString());
            AddChildXmlNode(itemNode, "OrderLineBomItemId", orderline.BomItemId);
            AddChildXmlNode(itemNode, "OrderLineGiftCardCode", orderline.GiftCardCode);
            AddChildXmlNode(itemNode, "OrderLineIsGiftCardDiscount", (!string.IsNullOrEmpty(orderline.GiftCardCode)).ToString(), isCustomField: true);
            AddChildXmlNode(itemNode, "OrderLineFieldValues", OrderLineFieldValuesToXml(orderline.OrderLineFieldValues).InnerXml);
            if (!settings.ErpControlsDiscount && orderline.IsDiscount())
            {
                AddChildXmlNode(itemNode, "OrderLineDiscountId", orderline.DiscountId);
                var discount = Services.Discounts.GetDiscount(Core.Converter.ToInt32(orderline.DiscountId));
                if (discount != null)
                {
                    AddChildXmlNode(itemNode, "OrderLineDiscountName", discount.Name);
                    AddChildXmlNode(itemNode, "OrderLineCampaignName", discount.CampaignName);
                    AddChildXmlNode(itemNode, "OrderLineDiscountType", discount.DiscountType.ToString());
                    AddChildXmlNode(itemNode, "OrderLineDiscountValue", Helpers.GetDiscountValue(currentSettings, discount, orderline, logger));
                }
            }

            if (currentSettings.AddOrderLinePartsToRequest)
            {
                foreach (var kitProductOrderLine in orderline.BomOrderLines)
                {
                    CreateOrderLineXml(currentSettings, orderLinesNode, kitProductOrderLine, settings, logger);
                }
            }
            if (!settings.GenerateXmlForHash)
            {
                NotificationManager.Notify(Notifications.OrderLine.OnAfterGenerateOrderLineXml, new Notifications.OrderLine.OnAfterGenerateOrderLineXmlArgs(orderline, itemNode, currentSettings, logger));
            }
        }

        private XmlDocument OrderLineFieldValuesToXml(OrderLineFieldValueCollection ofv)
        {
            var xml = new XmlDocument();
            var root = xml.CreateElement("OrderLineFieldValueCollection");
            xml.AppendChild(root);
            foreach (OrderLineFieldValue fieldValue in ofv)
            {
                var fieldValueNode = xml.CreateElement("OrderLineFieldValue");
                root.AppendChild(fieldValueNode);
                var systemNameNode = xml.CreateElement("OrderLineFieldSystemName");
                var valueNode = xml.CreateElement("Value");
                fieldValueNode.AppendChild(systemNameNode);
                fieldValueNode.AppendChild(valueNode);
                systemNameNode.AppendChild(xml.CreateTextNode(fieldValue.OrderLineFieldSystemName));
                valueNode.AppendChild(xml.CreateTextNode(fieldValue.Value));
            }
            return xml;
        }

        #region GiftCardOrderLine

        private double? _orderPriceWithoutVat = null;

        private double GetOrderPriceWithoutVat(Order order)
        {
            var orderPriceWithoutVat = 0d;
            var orderLines = order.OrderLines.Where(ol => !(ol.HasType(OrderLineType.Discount) && !string.IsNullOrEmpty(ol.GiftCardCode))).ToList();
            foreach (var ol in orderLines)
                orderPriceWithoutVat += ol.Price.PriceWithoutVAT;
            return orderPriceWithoutVat;
        }

        public double GetGiftCardAmountWithoutVat(OrderLine giftCardOrderLine)
        {
            if (_orderPriceWithoutVat.HasValue && _orderPriceWithoutVat != 0d)
            {
                if (_orderPriceWithoutVat >= giftCardOrderLine.UnitPrice.PriceWithoutVAT * -1)
                {
                    var unitPrice = giftCardOrderLine.UnitPrice;
                    _orderPriceWithoutVat += giftCardOrderLine.UnitPrice.PriceWithoutVAT;
                    return unitPrice.PriceWithoutVAT;
                }
                else
                {                    
                    var giftCardDiscount = _orderPriceWithoutVat.Value;
                    _orderPriceWithoutVat = 0d;
                    return giftCardDiscount > 0 ? giftCardDiscount * -1 : giftCardDiscount;
                }
            }
            return 0;
        }

        private static double MinusVat(double price, double percent)
        {
            return (double)((decimal)price / ((decimal)percent / 100M + 1M));
        }

        #endregion GiftCardOrderLine
    }
}