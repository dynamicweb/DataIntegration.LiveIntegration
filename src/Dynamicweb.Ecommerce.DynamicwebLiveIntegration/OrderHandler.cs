using Dynamicweb.Core;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Cache;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Discounts;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Environment;
using Dynamicweb.Extensibility.Notifications;
using Dynamicweb.Security.UserManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration
{
    /// <summary>
    /// Handler class to handle all interaction with the ERP.
    /// </summary>
    public static class OrderHandler
    {
        private static readonly string OrderErpCallFailed = "OrderHandler.ErpCallFailed";
        private static readonly string OrderErpCallCancelled = "OrderHandler.ErpCallCancelled";
        private static readonly string OrderErpCallSucceed = "OrderHandler.ErpCallSucceed";

        /// <summary>
        /// The order XML log folder
        /// </summary>
        private static readonly string OrderXmlLogFolder = "/Files/System/Log/LiveIntegration/OrderXml";


        /// <summary>
        /// Gets the cache level for order information.
        /// </summary>
        /// <value>The order cache level.</value>
        private static ResponseCacheLevel GetOrderCacheLevel(Settings settings)
        {
            string cacheLevelString = settings.OrderCacheLevel;
            return Helpers.GetEnumValueFromString(cacheLevelString, ResponseCacheLevel.Page);
        }

        /// <summary>
        /// Updates an order in the ERP.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="order">The order that must be synced with the ERP.</param>
        /// <param name="liveIntegrationSubmitType">Determines the origin of this submit such.</param>
        /// <param name="successOrderStateId">The order state that is applied to the order when it integrates successfully.</param>
        /// <param name="failedOrderStateId">The order state that is applied to the order when an error occurred during the integration.</param>
        /// <returns>Returns null if no communication has made, or bool if order has been updated successfully or not.</returns>
        public static bool? UpdateOrder(Settings settings, Order order, SubmitType liveIntegrationSubmitType, string successOrderStateId = null, string failedOrderStateId = null)
        {
            Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.OrderHandler.UpdateOrder START");
            if (!IsOrderUpdateAllowed(settings, order, liveIntegrationSubmitType))
            {
                Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.OrderHandler.UpdateOrder END");
                return null;
            }

            var orderId = order.Id ?? "ID is null";
            bool executingContextIsBackEnd = ExecutingContext.IsBackEnd();

            var logger = new Logger(settings);
            logger.Log(ErrorLevel.DebugInfo, $"Updating order with ID: {orderId}. Complete: {order.Complete}. Order submitted from the backend: {executingContextIsBackEnd}");

            // use current user if is not backend running or if the cart is Anonymous
            var user = UserManagementServices.Users.GetUserById(order.CustomerAccessUserId);

            /* create order: if it is false, you will get a calculate order from the ERP with the total prices */
            /* if it is true, then a new order will be created in the ERP */
            bool createOrder = order.Complete;

            /* Create order if the request is from Backend */
            if (executingContextIsBackEnd && !createOrder)
            {
                createOrder = true;
            }

            if (!settings.EnableCartCommunicationForAnonymousUsers && user == null)
            {
                logger.Log(ErrorLevel.DebugInfo, $"No user is currently logged in. Anonymous user cart is not allowed. Order = {orderId}");
                Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.OrderHandler.UpdateOrder END");
                return null;
            }

            // default states
            successOrderStateId ??= settings.OrderStateAfterExportSucceeded;

            failedOrderStateId ??= settings.OrderStateAfterExportFailed;
            var xmlGeneratorSettings = new OrderXmlGeneratorSettings
            {
                AddOrderLineFieldsToRequest = settings.AddOrderLineFieldsToRequest,
                AddOrderFieldsToRequest = settings.AddOrderFieldsToRequest,
                CreateOrder = createOrder,
                LiveIntegrationSubmitType = liveIntegrationSubmitType,
                ReferenceName = "OrdersPut",
                ErpControlsDiscount = settings.ErpControlsDiscount,
                ErpControlsShipping = settings.ErpControlsShipping,
                ErpShippingItemKey = settings.ErpShippingItemKey,
                ErpShippingItemType = settings.ErpShippingItemType,
                CalculateOrderUsingProductNumber = settings.CalculateOrderUsingProductNumber
            };
            var requestXml = new OrderXmlGenerator().GenerateOrderXml(settings, order, xmlGeneratorSettings, logger);
            xmlGeneratorSettings.GenerateXmlForHash = true;
            var requestXmlForHash = new OrderXmlGenerator().GenerateOrderXml(settings, order, xmlGeneratorSettings, logger);

            if (createOrder && settings.SaveCopyOfOrderXml && liveIntegrationSubmitType == SubmitType.LiveOrderOrCart)
            {
                SaveCopyOfXml(order.Id, requestXml, logger);
            }

            // calculate current hash
            string currentHash = Helpers.CalculateHash(requestXmlForHash);

            // get last hash
            string lastHash = GetLastOrderHash(settings);

            if (liveIntegrationSubmitType != SubmitType.ScheduledTask && liveIntegrationSubmitType != SubmitType.CaptureTask && 
                !string.IsNullOrEmpty(lastHash) && lastHash == currentHash)
            {
                // no changes to order
                Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.OrderHandler.UpdateOrder END");
                return true;
            }

            // save this hash for next calls
            SaveOrderHash(settings, currentHash);

            XmlDocument response = GetResponse(settings, requestXml, order, createOrder, logger, out bool? requestCancelled);
            if (response != null && !string.IsNullOrWhiteSpace(response.InnerXml))
            {
                bool processResponseResult = ProcessResponse(settings, response, order, createOrder, successOrderStateId, failedOrderStateId, logger);
                Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.OrderHandler.UpdateOrder END");
                return processResponseResult;
            }
            else
            {
                // error occurred                
                if (createOrder && (!requestCancelled.HasValue || !requestCancelled.Value))
                {
                    HandleIntegrationFailure(settings, order, failedOrderStateId, orderId, null, logger);
                    Services.OrderDebuggingInfos.Save(order, $"ERP communication failed with null response returned.", OrderErpCallFailed, DebuggingInfoType.Undefined);
                }

                Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.OrderHandler.UpdateOrder END");
                return false;
            }
        }

        /// <summary>
        /// Builds the XML copy path.
        /// </summary>
        /// <param name="orderId">The order identifier.</param>
        /// <param name="folder">The folder.</param>
        /// <returns>System.String.</returns>
        public static string BuildXmlCopyPath(string orderId, string folder)
        {
            return Path.Combine(folder, $"{orderId}.xml");
        }

        public static string GetLogFolderForXmlCopies(DateTime? date = null)
        {
            return GetLogFolderForXmlCopies(null, date);
        }

        /// <summary>
        /// Gets the log folder for XML copies.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>System.String.</returns>
        public static string GetLogFolderForXmlCopies(Logger logger, DateTime? date = null)
        {
            if (!date.HasValue)
            {
                date = DateTime.Now;
            }

            try
            {
                var logFolder = $"{OrderXmlLogFolder}/{date.Value.Year}/{date.Value:MM}";
                var logFolderPhysical = SystemInformation.MapPath(logFolder);

                if (!Directory.Exists(logFolderPhysical))
                {
                    Directory.CreateDirectory(logFolderPhysical);
                }

                return logFolderPhysical;
            }
            catch (Exception e)
            {
                logger?.Log(ErrorLevel.Error, "Error creating log folder for order XML files: " + e.Message);
                return string.Empty;
            }
        }

        /// <summary>
        /// Assigns the integration order identifier.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="orderNode">The order node.</param>
        private static void AssignIntegrationOrderId(Order order, XmlNode orderNode)
        {
            // search for IntegrationOrderID field in response otherwise use the OrderIntegrationOrderID
            XmlNode integrationIdNode = orderNode.SelectSingleNode("column [@columnName='OrderIntegrationOrderId']");

            // otherwise use the traditional OrderID
            if (string.IsNullOrEmpty(integrationIdNode?.InnerText))
            {
                integrationIdNode = orderNode.SelectSingleNode("column [@columnName='OrderId']");
            }

            if (!string.IsNullOrWhiteSpace(integrationIdNode?.InnerText))
            {
                order.IntegrationOrderId = integrationIdNode.InnerText;
                order.IsExported = true;
            }
        }

        /// <summary>
        /// Creates the order line.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="productNumber">The product identifier.</param>
        /// <returns>OrderLine.</returns>
        private static OrderLine CreateOrderLine(Order order, string productNumber, Logger logger)
        {
            if (order == null)
            {
                throw new ArgumentNullException(nameof(order));
            }

            if (string.IsNullOrEmpty(productNumber))
            {
                throw new ArgumentNullException(nameof(productNumber));
            }

            var product = Services.Products.GetProductByNumber(productNumber, order.LanguageId);
            if (product is null && !string.Equals(order.LanguageId, Services.Languages.GetDefaultLanguageId(), StringComparison.OrdinalIgnoreCase))
            {
                product = Services.Products.GetProductByNumber(productNumber, Services.Languages.GetDefaultLanguageId());
            }
            if (product == null)
            {
                logger.Log(ErrorLevel.Error, $"Cannot CreateOrderLine: No product found with ProductNumber = '{productNumber}' Order = {order.Id}");
                return null;
            }

            OrderLine orderLine = new OrderLine(order);
            Services.OrderLines.SetProductInformation(orderLine, product);
            orderLine.OrderLineType = OrderLineType.Product;

            order.OrderLines.Add(orderLine);

            return orderLine;
        }

        /// <summary>
        /// Gets the response.
        /// </summary>
        /// <param name="settings">Settings.</param> 
        /// <param name="requestXml">The request XML.</param>
        /// <param name="order">The order.</param>
        /// <param name="createOrder">if set to <c>true</c> [create order].</param>
        /// <returns>XmlDocument.</returns>
        private static XmlDocument GetResponse(Settings settings, string requestXml, Order order, bool createOrder, Logger logger, out bool? requestCancelled)
        {
            XmlDocument response = null;
            requestCancelled = null;

            string orderIdentifier = Helpers.OrderIdentifier(order);

            Dictionary<string, XmlDocument> responsesCache = ResponseCache.GetWebOrdersConnectorResponses(GetOrderCacheLevel(settings));

            if (!createOrder && responsesCache is object && responsesCache.ContainsKey(orderIdentifier))
            {
                response = responsesCache[orderIdentifier];
            }
            else
            {
                Notifications.Order.OnBeforeSendingOrderToErpArgs onBeforeSendingOrderToErpArgs = new Notifications.Order.OnBeforeSendingOrderToErpArgs(order, createOrder, settings, logger);
                NotificationManager.Notify(Notifications.Order.OnBeforeSendingOrderToErp, onBeforeSendingOrderToErpArgs);
                requestCancelled = onBeforeSendingOrderToErpArgs.Cancel;

                if (!onBeforeSendingOrderToErpArgs.Cancel)
                {
                    response = Connector.CalculateOrder(settings, requestXml, order, createOrder, out Exception error, logger);

                    if (createOrder && error != null)
                    {
                        Services.OrderDebuggingInfos.Save(order, $"ERP communication failed with error: {error}", OrderErpCallFailed, DebuggingInfoType.Undefined);
                    }

                    NotificationManager.Notify(Notifications.Order.OnAfterSendingOrderToErp, new Notifications.Order.OnAfterSendingOrderToErpArgs(order, createOrder, response, error, settings, logger));

                    if (responsesCache is object)
                    {
                        if (responsesCache.ContainsKey(orderIdentifier))
                        {
                            responsesCache.Remove(orderIdentifier);
                        }

                        if (response != null && !string.IsNullOrWhiteSpace(response.InnerXml))
                        {
                            responsesCache.Add(orderIdentifier, response);
                        }
                    }
                }
                else
                {
                    Services.OrderDebuggingInfos.Save(order, "Order not sent to ERP because a subscriber cancelled sending it", OrderErpCallCancelled, DebuggingInfoType.Undefined);
                }
            }

            return response;
        }

        /// <summary>
        /// Handles the integration failure.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="order">The order.</param>
        /// <param name="failedState">State of the failed.</param>
        /// <param name="orderId">The order identifier.</param>
        /// <param name="discountOrderLines">The discount order lines.</param>
        private static void HandleIntegrationFailure(Settings settings, Order order, string failedState, string orderId, OrderLineCollection discountOrderLines, Logger logger)
        {
            if (discountOrderLines != null && Global.EnableCartCommunication(settings, order.Complete))
            {
                RemoveDiscounts(order);
                order.OrderLines.Add(discountOrderLines);
            }

            logger.Log(ErrorLevel.Error, $"Order with ID '{orderId}' was not created in the ERP system.");
            if (Context.Current != null && Context.Current.Session != null)
            {
                Context.Current.Session["DynamicwebLiveIntegration.OrderExportFailed"] = true;
                Context.Current.Session["DynamicwebLiveIntegration.FailedOrderId"] = order.Id;
            }
            if (!settings.QueueOrdersToExport)
            {
                Services.Orders.DowngradeToCart(order);
                Common.Context.SetCart(order);
                //order.CartV2StepIndex = --order.CartV2StepIndex; // DW10 Api breaking change
                order.Complete = false;
            }

            if (!string.IsNullOrWhiteSpace(failedState))
            {
                order.StateId = failedState;
            }

            Services.Orders.Save(order);
        }

        /// <summary>
        /// Handles the integration success.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="successState">State of the success.</param>
        private static void HandleIntegrationSuccess(Order order, string successState)
        {
            if (!string.IsNullOrWhiteSpace(successState))
            {
                order.StateId = successState;
            }

            Services.Orders.Save(order);
            if (Context.Current != null && Context.Current.Session != null)
            {
                Context.Current.Session["DynamicwebLiveIntegration.OrderExportFailed"] = null;
                Context.Current.Session["DynamicwebLiveIntegration.FailedOrderId"] = null;
            }
        }

        private static bool IsOrderUpdateAllowed(Settings settings, Order order, SubmitType liveIntegrationSubmitType)
        {
            if (order == null
                || !order.OrderLines.Any()
                || (order.IsLedgerEntry && settings.SkipLedgerOrder)
                || (liveIntegrationSubmitType == SubmitType.LiveOrderOrCart && !string.IsNullOrEmpty(order.IntegrationOrderId)))
            {
                return false;
            }

            return true;
        }

        private static void UpdateOrderLinesPricesCurrency(Order order)
        {
            foreach (var orderLine in order.OrderLines)
            {
                if (orderLine.UnitPrice.Currency != order.Currency)
                    orderLine.UnitPrice.Currency = order.Currency;
                if (orderLine.Price.Currency != order.Currency)
                    orderLine.Price.Currency = order.Currency;
            }
        }

        /// <summary>
        /// Sets the price of the order with the values from the ERP.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="orderNode">The order node.</param>
        private static void OrderPriceCalculation(Settings settings, Order order, XmlNode orderNode, Logger logger, out bool updatePriceBeforeFeesFromOrderPrice)
        {
            updatePriceBeforeFeesFromOrderPrice = false;
            //If currency was changed order Price and order lines Price have prices in previous currency
            if (order.Price.Currency != order.Currency)
            {
                order.Price.Currency = order.Currency;
            }
            UpdateOrderLinesPricesCurrency(order);

            var orderPriceNode = orderNode.SelectSingleNode("column [@columnName='OrderPrice']") ?? orderNode.SelectSingleNode("column [@columnName='OrderPriceWithVat']");
            if (orderPriceNode != null)
            {
                order.Price.PriceWithVAT = Helpers.ToDouble(settings, logger, orderPriceNode.InnerText);
            }

            var orderPriceWithoutVatNode = orderNode.SelectSingleNode("column [@columnName='OrderPriceWithoutVat']");

            if (orderPriceWithoutVatNode != null)
            {
                order.Price.PriceWithoutVAT = Helpers.ToDouble(settings, logger, orderPriceWithoutVatNode.InnerText);
            }

            if (order.Price.PriceWithVAT > 0 && order.Price.PriceWithoutVAT > 0)
            {
                order.Price.VAT = order.Price.PriceWithVAT - order.Price.PriceWithoutVAT;
            }

            var orderPriceBeforeFeesWithVat = orderNode.SelectSingleNode("column [@columnName='OrderPriceBeforeFeesWithVat']");

            if (orderPriceBeforeFeesWithVat != null)
            {
                order.PriceBeforeFees.PriceWithVAT = Helpers.ToDouble(settings, logger, orderPriceBeforeFeesWithVat.InnerText);
            }
            else
            {
                order.PriceBeforeFees.PriceWithVAT = order.Price.PriceWithVAT;
                updatePriceBeforeFeesFromOrderPrice = true;
            }

            var orderPriceBeforeFeesWithoutVat = orderNode.SelectSingleNode("column [@columnName='OrderPriceBeforeFeesWithoutVat']");

            if (orderPriceBeforeFeesWithoutVat != null)
            {
                order.PriceBeforeFees.PriceWithoutVAT = Helpers.ToDouble(settings, logger, orderPriceBeforeFeesWithoutVat.InnerText);
            }
            else
            {
                order.PriceBeforeFees.PriceWithoutVAT = order.Price.PriceWithoutVAT;
                updatePriceBeforeFeesFromOrderPrice = true;
            }

            if (order.PriceBeforeFees.PriceWithVAT > 0 && order.PriceBeforeFees.PriceWithoutVAT > 0)
            {
                order.PriceBeforeFees.VAT = order.PriceBeforeFees.PriceWithVAT - order.PriceBeforeFees.PriceWithoutVAT;
            }
        }

        /// <summary>
        /// Processes the discount order line.
        /// </summary>
        /// <param name="settings">Settings.</param> 
        /// <param name="order">The order.</param>
        /// <param name="discountOrderLines">The discount order lines.</param>
        /// <param name="orderLineNode">The order line node.</param>
        /// <param name="orderLineType">Type of the order line.</param>
        private static void ProcessDiscountOrderLine(Settings settings, Order order, OrderLineCollection discountOrderLines, XmlNode orderLineNode, string orderLineType, Logger logger, List<string> orderLineIds, OrderLineFieldCollection allOrderLineFields)
        {
            string orderLineId = orderLineNode.SelectSingleNode("column [@columnName='OrderLineId']")?.InnerText;

            try
            {
                var orderLine = new OrderLine(order)
                {
                    OrderLineType = Services.OrderLines.GetOrderLineType(orderLineType)
                };

                if (!settings.ErpControlsDiscount)
                {
                    orderLine.DiscountId = orderLineNode.SelectSingleNode("column [@columnName='OrderLineDiscountId']")?.InnerText;
                }

                OrderLine parentLine = null;
                OrderLine parentLineWithVariant = null;
                bool useUnitPrices = settings.UseUnitPrices;

                if (orderLine.OrderLineType == OrderLineType.ProductDiscount)
                {
                    string parentProductId = orderLineNode.SelectSingleNode("column [@columnName='OrderLineProductNumber']")?.InnerText;
                    if (!string.IsNullOrEmpty(parentProductId))
                    {
                        string parentProductVariantId = orderLineNode.SelectSingleNode("column [@columnName='OrderLineProductVariantId']")?.InnerText;
                        string unitId = null;
                        if (useUnitPrices)
                        {
                            unitId = orderLineNode.SelectSingleNode("column [@columnName='OrderLineUnitId']")?.InnerText;
                        }
                        foreach (var productOrderLine in order.OrderLines)
                        {                            
                            string id = settings.CalculateOrderUsingProductNumber ? productOrderLine.ProductNumber : productOrderLine.ProductId;
                            if (string.Compare(id, parentProductId, StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                bool found = false;
                                if (useUnitPrices)
                                {
                                    if ((string.IsNullOrEmpty(unitId) && string.IsNullOrEmpty(productOrderLine.UnitId)) ||
                                        string.Compare(productOrderLine.UnitId, unitId, StringComparison.OrdinalIgnoreCase) == 0)
                                    {
                                        parentLine = productOrderLine;
                                        found = true;
                                    }
                                }
                                else
                                {
                                    parentLine = productOrderLine;
                                    found = true;
                                }
                                if (found && !string.IsNullOrEmpty(parentProductVariantId) && string.Equals(productOrderLine.ProductVariantId, parentProductVariantId, StringComparison.OrdinalIgnoreCase))
                                {
                                    parentLineWithVariant = productOrderLine;
                                }
                            }
                        }
                    }
                }

                parentLine = parentLineWithVariant ?? parentLine;
                if (parentLine != null && string.IsNullOrEmpty(parentLine.Id))
                {
                    Services.OrderLines.Save(parentLine);
                    orderLineIds.Add(parentLine.Id);
                }

                if (!string.IsNullOrEmpty(parentLine?.Id))
                {
                    orderLine.ParentLineId = parentLine.Id;
                }

                orderLine.ProductName = DiscountTranslation.GetDiscountName(settings, orderLineNode, orderLine);
                if (settings.ErpControlsDiscount)
                {
                    orderLine.AllowOverridePrices = true;
                }

                double? value = ReadDouble(settings, orderLineNode, "column [@columnName='OrderLineQuantity']", logger);
                if (value.HasValue)
                {
                    orderLine.Quantity = value.Value;
                }
                else
                {
                    orderLine.Quantity = 1;
                }

                double? unitPriceVat = ReadDouble(settings, orderLineNode, "column [@columnName='OrderLineUnitPriceVat']", logger);
                SetPrice(
                    orderLine.UnitPrice,
                    ReadDouble(settings, orderLineNode, "column [@columnName='OrderLineUnitPriceWithVat']", logger),
                    ReadDouble(settings, orderLineNode, "column [@columnName='OrderLineUnitPriceWithoutVat']", logger),
                    unitPriceVat);

                double? priceVat = ReadDouble(settings, orderLineNode, "column [@columnName='OrderLinePriceVat']", logger);
                SetPrice(
                    orderLine.Price,
                    ReadDouble(settings, orderLineNode, "column [@columnName='OrderLinePriceWithVat']", logger),
                    ReadDouble(settings, orderLineNode, "column [@columnName='OrderLinePriceWithoutVat']", logger),
                    priceVat);

                orderLine.UnitPrice.PriceWithoutVAT = orderLine.UnitPrice.PriceWithoutVAT > 0 ? -orderLine.UnitPrice.PriceWithoutVAT : orderLine.UnitPrice.PriceWithoutVAT;
                orderLine.UnitPrice.PriceWithVAT = orderLine.UnitPrice.PriceWithVAT > 0 ? -orderLine.UnitPrice.PriceWithVAT : orderLine.UnitPrice.PriceWithVAT;
                if (unitPriceVat.HasValue)
                {
                    orderLine.UnitPrice.VAT = orderLine.UnitPrice.VAT > 0 ? -orderLine.UnitPrice.VAT : orderLine.UnitPrice.VAT;
                }
                orderLine.Price.PriceWithVAT = orderLine.Price.PriceWithVAT > 0 ? -orderLine.Price.PriceWithVAT : orderLine.Price.PriceWithVAT;
                orderLine.Price.PriceWithoutVAT = orderLine.Price.PriceWithoutVAT > 0 ? -orderLine.Price.PriceWithoutVAT : orderLine.Price.PriceWithoutVAT;
                if (priceVat.HasValue)
                {
                    orderLine.Price.VAT = orderLine.Price.VAT > 0 ? -orderLine.Price.VAT : orderLine.Price.VAT;
                }

                ProcessOrderLineCustomFields(settings, orderLine, allOrderLineFields, orderLineNode);

                discountOrderLines.Add(orderLine);
            }
            catch (Exception ex)
            {
                logger.Log(ErrorLevel.Error, $"Error processing order line. Error: '{ex.Message}' OrderLineId = {orderLineId}.");
                throw;
            }
        }

        /// <summary>
        /// Processes the order lines.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="response">The response.</param>
        /// <param name="order">The order.</param>
        /// <param name="discountOrderLines">The discount order lines.</param>        
        private static void ProcessOrderLines(Settings settings, XmlDocument response, Order order, OrderLineCollection discountOrderLines, Logger logger)
        {
            XmlNodeList orderLinesNodes = response.SelectNodes("//item [@table='EcomOrderLines']");

            // Process OrderLines
            if (orderLinesNodes != null)
            {
                List<string> orderLineIds = new List<string>();
                List<OrderLine> orderLines = order.OrderLines.ToList();
                OrderLineFieldCollection allOrderLineFields = null;
                if (settings.AddOrderLineFieldsToRequest)
                {
                    allOrderLineFields = Services.OrderLineFields.GetOrderLineFields();
                }
                bool processDiscounts = (settings.ErpControlsDiscount || !order.Complete);
                Dictionary<string, OrderLine> responseIdOrderLineDictionary = new Dictionary<string, OrderLine>();

                foreach (XmlNode orderLineNode in orderLinesNodes)
                {
                    XmlNode orderLineTypeNode = orderLineNode.SelectSingleNode("column [@columnName='OrderLineType']") ?? orderLineNode.SelectSingleNode("column [@columnName='OrderLineTypeId']");
                    string orderLineType = orderLineTypeNode?.InnerText;
                    if (string.IsNullOrWhiteSpace(orderLineType) || orderLineType == "0" || orderLineType == "2") // 2=Fixed
                    {
                        ProcessProductOrderLine(settings, order, orderLineIds, orderLines, allOrderLineFields, orderLineNode, responseIdOrderLineDictionary, logger);
                    }

                    // 1=order discount, 3=Product Discount                    
                    if (processDiscounts && (orderLineType == "1" || orderLineType == "3"))
                    {
                        ProcessDiscountOrderLine(settings, order, discountOrderLines, orderLineNode, orderLineType, logger, orderLineIds, allOrderLineFields);
                    }

                    // 4=Product Tax                    
                    if (orderLineType == "4")
                    {
                        ProcessTaxOrderLine(settings, order, orderLineNode, logger, orderLineIds, allOrderLineFields);
                    }
                }

                bool keepDiscountOrderLines = !settings.ErpControlsDiscount && order.Complete;
                // Remove deleted OrderLines
                for (int i = order.OrderLines.Count - 1; i >= 0; i--)
                {
                    var orderLine = order.OrderLines[i];
                    if ((string.IsNullOrWhiteSpace(orderLine.Id) && !orderLine.IsDiscount()) ||
                        orderLineIds.Contains(orderLine.Id))
                    {
                        continue;
                    }
                    if (keepDiscountOrderLines && orderLine.IsDiscount())
                    {
                        continue;
                    }
                    order.OrderLines.Remove(orderLine);
                    Services.OrderLines.Delete(orderLine.Id);
                }
                MergeOrderLines(settings, order);
            }
        }

        private static void ProcessTaxOrderLine(Settings settings, Order order, XmlNode orderLineNode, Logger logger, List<string> orderLineIds, OrderLineFieldCollection allOrderLineFields)
        {
            string orderLineId = orderLineNode.SelectSingleNode("column [@columnName='OrderLineId']")?.InnerText;

            try
            {
                var orderLine = new OrderLine(order)
                {
                    OrderLineType = OrderLineType.Tax
                };

                OrderLine parentLine = null;
                OrderLine parentLineWithVariant = null;
                bool useUnitPrices = settings.UseUnitPrices;

                string parentProductId = orderLineNode.SelectSingleNode("column [@columnName='OrderLineProductNumber']")?.InnerText;
                if (!string.IsNullOrEmpty(parentProductId))
                {
                    string parentProductVariantId = orderLineNode.SelectSingleNode("column [@columnName='OrderLineProductVariantId']")?.InnerText;
                    string unitId = null;
                    if (useUnitPrices)
                    {
                        unitId = orderLineNode.SelectSingleNode("column [@columnName='OrderLineUnitId']")?.InnerText;
                    }
                    foreach (var productOrderLine in order.OrderLines)
                    {
                        string id = settings.CalculateOrderUsingProductNumber ? productOrderLine.ProductNumber : productOrderLine.ProductId;
                        if (string.Compare(id, parentProductId, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            bool found = false;
                            if (useUnitPrices)
                            {
                                if ((string.IsNullOrEmpty(unitId) && string.IsNullOrEmpty(productOrderLine.UnitId)) ||
                                    string.Compare(productOrderLine.UnitId, unitId, StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    parentLine = productOrderLine;
                                    found = true;
                                }
                            }
                            else
                            {
                                parentLine = productOrderLine;
                                found = true;
                            }
                            if (found && !string.IsNullOrEmpty(parentProductVariantId) && string.Equals(productOrderLine.ProductVariantId, parentProductVariantId, StringComparison.OrdinalIgnoreCase))
                            {
                                parentLineWithVariant = productOrderLine;
                            }
                        }
                    }
                }

                parentLine = parentLineWithVariant ?? parentLine;
                if (parentLine != null && string.IsNullOrEmpty(parentLine.Id))
                {
                    Services.OrderLines.Save(parentLine);
                    orderLineIds.Add(parentLine.Id);
                }

                if (!string.IsNullOrEmpty(parentLine?.Id))
                {
                    orderLine.ParentLineId = parentLine.Id;
                }

                orderLine.AllowOverridePrices = true;

                double? value = ReadDouble(settings, orderLineNode, "column [@columnName='OrderLineQuantity']", logger);
                if (value.HasValue)
                {
                    orderLine.Quantity = value.Value;
                }
                else
                {
                    orderLine.Quantity = 1;
                }

                string productName = orderLineNode?.SelectSingleNode("column [@columnName='OrderLineProductName']")?.InnerText;
                if (!string.IsNullOrWhiteSpace(productName))
                {
                    orderLine.ProductName = productName;
                }

                double? unitPriceVat = ReadDouble(settings, orderLineNode, "column [@columnName='OrderLineUnitPriceVat']", logger);
                SetPrice(
                    orderLine.UnitPrice,
                    ReadDouble(settings, orderLineNode, "column [@columnName='OrderLineUnitPriceWithVat']", logger),
                    ReadDouble(settings, orderLineNode, "column [@columnName='OrderLineUnitPriceWithoutVat']", logger),
                    unitPriceVat);

                double? priceVat = ReadDouble(settings, orderLineNode, "column [@columnName='OrderLinePriceVat']", logger);
                SetPrice(
                    orderLine.Price,
                    ReadDouble(settings, orderLineNode, "column [@columnName='OrderLinePriceWithVat']", logger),
                    ReadDouble(settings, orderLineNode, "column [@columnName='OrderLinePriceWithoutVat']", logger),
                    priceVat);

                ProcessOrderLineCustomFields(settings, orderLine, allOrderLineFields, orderLineNode);

                order.OrderLines.Add(orderLine);
            }
            catch (Exception ex)
            {
                logger.Log(ErrorLevel.Error, $"Error processing order line. Error: '{ex.Message}' OrderLineId = {orderLineId}.");
                throw;
            }
        }

        /// <summary>
        /// Processes the product order line.
        /// </summary>
        /// <param name="settings">Settings.</param> 
        /// <param name="order">The order.</param>
        /// <param name="orderLineIds">The order line IDs.</param>
        /// <param name="orderLines">The order lines.</param>
        /// <param name="allOrderLineFields">All order line fields.</param>
        /// <param name="orderLineNode">The order line node.</param>
        private static void ProcessProductOrderLine(Settings settings, Order order, List<string> orderLineIds, List<OrderLine> orderLines, OrderLineFieldCollection allOrderLineFields, XmlNode orderLineNode, Dictionary<string, OrderLine> responseIdOrderLineDictionary, Logger logger)
        {
            string productNumber = orderLineNode.SelectSingleNode("column [@columnName='OrderLineProductNumber']")?.InnerText;

            try
            {
                if (!string.IsNullOrWhiteSpace(productNumber))
                {
                    OrderLine orderLine = orderLines.FirstOrDefault(ol => ol.ProductNumber == productNumber);

                    if (orderLine == null && settings.AddOrderLinePartsToRequest)
                    {
                        orderLine = GetBomOrderLine(orderLineNode, responseIdOrderLineDictionary, productNumber);
                    }
                    if (orderLine != null)
                    {
                        // Remove found line for getting next line with same ProductNumber
                        orderLines.Remove(orderLine);
                    }
                    else
                    {
                        // Create an OrderLine if it doesn't exist
                        orderLine = CreateOrderLine(order, productNumber, logger);

                        if (orderLine == null)
                        {
                            return;
                        }
                    }
                    if (!orderLine.Bom && settings.AddOrderLinePartsToRequest)
                    {
                        string id = orderLineNode.SelectSingleNode("column [@columnName='OrderLineId']")?.InnerText;
                        if (!string.IsNullOrEmpty(id) && !responseIdOrderLineDictionary.ContainsKey(id))
                            responseIdOrderLineDictionary.Add(id, orderLine);
                    }

                    if (!string.IsNullOrWhiteSpace(orderLine.Id))
                    {
                        orderLineIds.Add(orderLine.Id);
                    }

                    // Set standard values on OrderLines
                    orderLine.AllowOverridePrices = true;

                    var doubleValue = ReadDouble(settings, orderLineNode, "column [@columnName='OrderLineQuantity']", logger);
                    if (doubleValue.HasValue)
                    {
                        orderLine.Quantity = doubleValue.Value;
                    }
                    var unitId = orderLineNode.SelectSingleNode("column [@columnName='OrderLineUnitId']")?.InnerText;
                    if (!string.IsNullOrWhiteSpace(unitId))
                    {
                        orderLine.UnitId = unitId;
                    }

                    // order line unit price
                    PriceInfo unitPrice = new PriceInfo(order.Currency);
                    SetPrice(
                        unitPrice,
                        ReadDouble(settings, orderLineNode, "column [@columnName='OrderLineUnitPriceWithVat']", logger),
                        ReadDouble(settings, orderLineNode, "column [@columnName='OrderLineUnitPriceWithoutVat']", logger),
                        ReadDouble(settings, orderLineNode, "column [@columnName='OrderLineUnitPriceVat']", logger));

                    Services.OrderLines.SetUnitPrice(orderLine, unitPrice, false);

                    if (settings.SetOrderlineFixed)
                    {
                        orderLine.OrderLineType = OrderLineType.Fixed;
                    }

                    // order line price
                    SetPrice(
                        orderLine.Price,
                        ReadDouble(settings, orderLineNode, "column [@columnName='OrderLinePriceWithVat']", logger),
                        ReadDouble(settings, orderLineNode, "column [@columnName='OrderLinePriceWithoutVat']", logger),
                        ReadDouble(settings, orderLineNode, "column [@columnName='OrderLinePriceVat']", logger));

                    UpdateOrderLinesPricesCurrency(order);

                    // Set OrderLineCustomFields values
                    ProcessOrderLineCustomFields(settings, orderLine, allOrderLineFields, orderLineNode);
                }
            }
            catch (Exception ex)
            {
                logger.Log(ErrorLevel.Error, $"Error processing order line. Error: '{ex.Message}' productNumber = {productNumber}.");
                throw;
            }
        }

        /// <summary>
        /// Processes the response.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="response">The response.</param>
        /// <param name="order">The order.</param>
        /// <param name="createOrder">if set to <c>true</c> [create order].</param>
        /// <param name="successState">State of the success.</param>
        /// <param name="failedState">State of the failed.</param>
        /// <returns><c>true</c> if response was processed successfully, <c>false</c> otherwise.</returns>
        private static bool ProcessResponse(Settings settings, XmlDocument response, Order order, bool createOrder, string successState, string failedState, Logger logger)
        {
            var orderId = order == null ? "is null" : order.Id ?? "ID is null";
            if (response == null || order == null)
            {
                if (createOrder)
                {
                    // if must create order and no response or invalid order fail to sync
                    logger.Log(ErrorLevel.Error, $"Response CreateOrder is null. Order = {orderId}");
                    return false;
                }

                // nothing to do so work done
                return true;
            }

            try
            {
                XmlNode orderNode = response.SelectSingleNode("//item [@table='EcomOrders']");
                PriceInfo shippingFeeSentInRequest = null;
                if (!createOrder && !settings.ErpControlsShipping && !string.IsNullOrEmpty(order.ShippingMethodId))
                {
                    shippingFeeSentInRequest = order.ShippingFee;
                }
                if (!createOrder && settings.ErpControlsDiscount)
                    order.IsPriceCalculatedByProvider = true;

                SetCustomOrderFields(settings, order, orderNode);

                var discountOrderLines = new OrderLineCollection(order);
                bool enableCartCommunication = Global.EnableCartCommunication(settings, order.Complete);
                bool updatePriceBeforeFeesFromOrderPrice = false;

                if (enableCartCommunication)
                {
                    ProcessOrderLines(settings, response, order, discountOrderLines, logger);

                    if (!order.Complete || settings.ErpControlsDiscount)
                    {
                        if (settings.ErpControlsDiscount)
                        {
                            foreach (var discountLine in discountOrderLines)
                                order.OrderLines.Add(discountLine, false);

                            SetOrderPrices(order, orderNode, settings, logger, orderId, out updatePriceBeforeFeesFromOrderPrice);
                            SetTotalOrderDiscount(order);

                            // When GetCart DwApi request is executed and ERP controls discounts:
                            // old discount lines are deleted and new discounts are not saved
                            // So at that time in backend the order lines will look incorrect, so order needs to be saved to keep discounts: https://vimeo.com/724424362/132443e631
                            if (!settings.UseUnitPrices && discountOrderLines.Count > 0 &&
                                Context.Current?.Request?.RawUrl is object &&
                                Context.Current.Request.RawUrl.Contains($"/dwapi/ecommerce/carts/{order.Secret}"))
                            {
                                Services.Orders.Save(order);
                            }
                        }
                        else if (!order.Complete)
                        {
                            SetOrderPrices(order, orderNode, settings, logger, orderId, out updatePriceBeforeFeesFromOrderPrice);
                            Services.Orders.CalculateDiscounts(order);
                        }
                        else
                        {
                            SetOrderPrices(order, orderNode, settings, logger, orderId, out updatePriceBeforeFeesFromOrderPrice);
                        }
                    }
                    else
                    {
                        SetOrderPrices(order, orderNode, settings, logger, orderId, out updatePriceBeforeFeesFromOrderPrice);
                    }
                    LiveShippingFeeProvider.ProcessShipping(settings, order, orderNode, logger);
                }
                else
                {
                    SetOrderPrices(order, orderNode, settings, logger, orderId, out updatePriceBeforeFeesFromOrderPrice);
                }

                if (createOrder)
                {
                    AssignIntegrationOrderId(order, orderNode);
                    bool.TryParse(orderNode?.SelectSingleNode("column [@columnName='OrderCreated']")?.InnerText, out bool orderCreatedSuccessfully);

                    if (!orderCreatedSuccessfully)
                    {
                        HandleIntegrationFailure(settings, order, failedState, orderId, discountOrderLines, logger);
                    }
                    else
                    {
                        SetShippingWarning(settings, order, orderNode);
                        HandleIntegrationSuccess(order, successState);
                    }
                }
                else
                {
                    if (!settings.ErpControlsShipping && shippingFeeSentInRequest != null)
                    {
                        UpdateDynamicwebShipping(order, orderNode, shippingFeeSentInRequest, settings, logger, updatePriceBeforeFeesFromOrderPrice);
                    }
                    if (enableCartCommunication)
                    {
                        Services.Orders.Save(order);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(ErrorLevel.Error, $"Error processing response. Error: '{ex.Message}' Order = {orderId}. Stack: {ex.StackTrace}.");

                if (createOrder)
                {
                    Services.OrderDebuggingInfos.Save(order, $"ERP communication failed with error: {ex}", OrderErpCallFailed, DebuggingInfoType.Undefined);
                }

                return false;
            }

            if (createOrder)
            {
                Services.OrderDebuggingInfos.Save(order, $"Order saved in ERP successfully.", OrderErpCallSucceed, DebuggingInfoType.Undefined);
            }
            return true;
        }

        private static void SetOrderPrices(Order order, XmlNode orderNode, Settings settings, Logger logger, string orderId, out bool updatePriceBeforeFeesFromOrderPrice)
        {
            updatePriceBeforeFeesFromOrderPrice = false;
            // Set Order prices
            order.AllowOverridePrices = settings.ErpControlsDiscount;
            order.DisableDiscountCalculation = settings.ErpControlsDiscount;
            try
            {
                SetPrices(settings, order, orderNode, logger, out updatePriceBeforeFeesFromOrderPrice);
                SetCustomerNumber(order, orderNode);
            }
            catch (Exception ex)
            {
                logger.Log(ErrorLevel.Error, $"Exception setting prices: {ex.Message} Order = {orderId}");
            }
        }

        /// <summary>
        /// Reads a double from the XML node.
        /// </summary>
        /// <param name="elementNode">The element node.</param>
        /// <param name="field">The field.</param>
        /// <returns>System.Nullable&lt;System.Double&gt;.</returns>
        private static double? ReadDouble(Settings settings, XmlNode elementNode, string field, Logger logger)
        {
            XmlNode xValue = elementNode.SelectSingleNode(field);

            if (xValue == null)
            {
                return null;
            }

            return Helpers.ToDouble(settings, logger, xValue.InnerText);
        }

        /// <summary>
        /// Removes the discounts.
        /// </summary>
        /// <param name="order">The order.</param>
        private static void RemoveDiscounts(Order order)
        {
            var lines = order.OrderLines.Where(ol => ol.IsDiscount()).ToList();
            foreach (var orderLine in lines)
            {
                order.OrderLines.Remove(orderLine);
                Services.OrderLines.Delete(orderLine.Id);
            }
        }

        /// <summary>
        /// Saves a copy of the order XML to a custom log folder under the main LiveIntegration log folder.
        /// </summary>
        /// <param name="orderId">The ID of the order being saved; used in the file name.</param>
        /// <param name="requestXml">The order XML to save.</param>
        private static void SaveCopyOfXml(string orderId, string requestXml, Logger logger)
        {
            try
            {
                var folder = GetLogFolderForXmlCopies(logger);
                string file = BuildXmlCopyPath(orderId, folder);
                var doc = new XmlDocument();
                doc.LoadXml(requestXml);
                File.WriteAllText(file, doc.Beautify());
            }
            catch (Exception e)
            {
                logger.Log(ErrorLevel.Error, "Error writing copy of XML to log folder: " + e.Message);
            }
        }

        /// <summary>
        /// Sets the customer number.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="orderNode">The order node.</param>
        private static void SetCustomerNumber(Order order, XmlNode orderNode)
        {
            var orderCustomerNumber = orderNode.SelectSingleNode("column [@columnName='OrderCustomerNumber']");
            if (orderCustomerNumber != null)
            {
                var newCustomerNumber = orderCustomerNumber.InnerText;

                if (string.Compare(order.CustomerNumber, newCustomerNumber, StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    order.CustomerNumber = newCustomerNumber;
                }
            }
        }

        /// <summary>
        /// Sets the custom order fields.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="order">The order.</param>
        /// <param name="orderNode">The order node.</param>
        private static void SetCustomOrderFields(Settings settings, Order order, XmlNode orderNode)
        {
            // Set OrderCustomFields values
            if (!settings.AddOrderFieldsToRequest || order.OrderFieldValues.Count <= 0)
            {
                return;
            }

            foreach (var orderFieldValue in order.OrderFieldValues)
            {
                var fieldNode = orderNode.SelectSingleNode(
                    $"column [@columnName='{orderFieldValue.OrderField.SystemName}']");
                if (fieldNode != null)
                {
                    orderFieldValue.Value = fieldNode.InnerText;
                }
            }
        }

        private static void SetPrice(PriceInfo price, double? priceWithVat, double? priceWithoutVat, double? priceVat)
        {
            if (priceWithVat.HasValue && priceWithoutVat.HasValue && priceVat.HasValue)
            {
                price.VAT = priceVat.Value;
                price.PriceWithVAT = priceWithVat.Value;
                price.PriceWithoutVAT = priceWithoutVat.Value;
            }
            else
            {
                if (priceVat.HasValue)
                {
                    price.VAT = priceVat.Value;
                }

                if (priceWithVat.HasValue)
                {
                    price.PriceWithVAT = priceWithVat.Value;
                    price.PriceWithoutVAT = price.PriceWithVAT - price.VAT;
                }

                if (priceWithoutVat.HasValue)
                {
                    price.PriceWithoutVAT = priceWithoutVat.Value;
                    price.PriceWithVAT = price.PriceWithoutVAT + price.VAT;
                }
            }
        }

        /// <summary>
        /// Sets the prices.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <param name="orderNode">The order node.</param>
        private static void SetPrices(Settings settings, Order order, XmlNode orderNode, Logger logger, out bool updatePriceBeforeFeesFromOrderPrice)
        {
            OrderPriceCalculation(settings, order, orderNode, logger, out updatePriceBeforeFeesFromOrderPrice);
            var orderDiscount = orderNode.SelectSingleNode("column [@columnName='OrderSalesDiscount']");
            if (orderDiscount != null)
            {
                order.SalesDiscount = Helpers.ToDouble(settings, logger, orderDiscount.InnerText);
            }
        }

        /// <summary>
        /// Sets the shipping warning.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="order">The order.</param>
        /// <param name="orderNode">The order node.</param>
        private static void SetShippingWarning(Settings settings, Order order, XmlNode orderNode)
        {
            if (!settings.ErpControlsShipping)
            {
                var node = orderNode.SelectSingleNode("column [@columnName='OrderShippingWarning']");
                if (node != null)
                {
                    var warning = node.InnerText;
                    if (!string.IsNullOrEmpty(warning))
                    {
                        order.Comment = warning;
                    }
                }
            }
        }

        private static void UpdateDynamicwebShipping(Order order, XmlNode orderNode, PriceInfo shippingFeeSentInRequest, Settings settings, Logger logger, bool updatePriceBeforeFeesFromOrderPrice)
        {
            Notifications.Order.OnBeforeUpdateDynamicwebShippingArgs onBeforeUpdateDynamicwebShippingArgs = new Notifications.Order.OnBeforeUpdateDynamicwebShippingArgs(order, orderNode, shippingFeeSentInRequest, settings, logger);
            NotificationManager.Notify(Notifications.Order.OnBeforeUpdateDynamicwebShipping, onBeforeUpdateDynamicwebShippingArgs);
            if (!onBeforeUpdateDynamicwebShippingArgs.StopDefaultDynamicwebShippingProcessing)
            {
                string shippingMethodId = orderNode.SelectSingleNode("column [@columnName='OrderShippingMethodId']")?.InnerText;
                //Check to ensure we are changing the prices for our standard BC/NAV units and not breaking other ERP implementations
                if (string.Equals(order.ShippingMethodId, shippingMethodId))
                {
                    //Calculate new shipping fee based on the update orderlines from the ERP response
                    var newShippingFee = LiveShippingFeeProvider.GetShippingFee(order);
                    //Standard BC/NAV codeunits add the shipping fee from Request to the total order price in the Response
                    //But after ERP response the Dynamicweb shipping can be changed so it is needed to correct the order price
                    var correctionFee = newShippingFee.Substract(shippingFeeSentInRequest);
                    if (correctionFee.Price != 0)
                    {
                        order.Price.PriceWithVAT = order.Price.PriceWithVAT + correctionFee.PriceWithVAT;
                        order.Price.PriceWithoutVAT = order.Price.PriceWithoutVAT + correctionFee.PriceWithoutVAT;
                        order.Price.VAT = order.Price.PriceWithVAT - order.Price.PriceWithoutVAT;
                        if (updatePriceBeforeFeesFromOrderPrice)
                        {
                            order.PriceBeforeFees.PriceWithVAT = order.Price.PriceWithVAT;
                            order.PriceBeforeFees.PriceWithoutVAT = order.Price.PriceWithoutVAT;
                            order.PriceBeforeFees.VAT = order.Price.VAT;
                        }
                    }
                }
            }
        }

        private static OrderLine GetBomOrderLine(XmlNode orderLineNode, Dictionary<string, OrderLine> responseIdOrderLineDictionary, string productNumber)
        {
            OrderLine orderLine = null;
            string parentLineId = orderLineNode.SelectSingleNode("column [@columnName='OrderLineParentLineId']")?.InnerText;
            if (!string.IsNullOrEmpty(parentLineId) && responseIdOrderLineDictionary.TryGetValue(parentLineId, out var parentLine))
            {
                if (parentLine != null)
                {
                    orderLine = parentLine.BomOrderLines.FirstOrDefault(l => l.ProductNumber == productNumber);
                    if (orderLine != null)
                    {
                        if (parentLine.Price == null || parentLine.Price.Price == 0d)
                            parentLine.AllowOverridePrices = false;
                    }
                }
            }
            return orderLine;
        }

        private static void MergeOrderLines(Settings settings, Order order)
        {
            if (order.Complete || !settings.AddOrderLineFieldsToRequest || settings.ErpControlsDiscount || order.OrderLines.Count <= 1)
            {
                return;
            }
            var newLines = order.OrderLines.Where(l => string.IsNullOrEmpty(l.Id)).ToList();
            if (!newLines.Any())
            {
                return;
            }
            var mergedLines = new List<OrderLine>();
            foreach (var newLine in newLines)
            {
                foreach (OrderLine theOrderLine in order.OrderLines.Where(l => !string.IsNullOrEmpty(l.Id)).ToList())
                {
                    if (!string.IsNullOrEmpty(theOrderLine.DiscountId) || theOrderLine.IsDiscount() || !theOrderLine.IsProduct())
                    {
                        continue;
                    }
                    if (Services.OrderLines.CanBeMerged(theOrderLine, newLine))
                    {
                        theOrderLine.Quantity += newLine.Quantity;
                        theOrderLine.AllowOverridePrices = false;
                        for (int i = 0, loopTo = theOrderLine.BomOrderLines.Count - 1; i <= loopTo; i++)
                            theOrderLine.BomOrderLines[i].Quantity += newLine.BomOrderLines[i].Quantity;
                        mergedLines.Add(newLine);
                        break;
                    }
                }
            }
            if (mergedLines.Any())
            {
                foreach (var mergedLine in mergedLines)
                {
                    order.OrderLines.Remove(mergedLine);
                }
                SaveOrderHash(settings, string.Empty);
                Services.Orders.ClearCachedPrices(order);
            }
        }

        private static void ProcessOrderLineCustomFields(Settings settings, OrderLine orderLine, OrderLineFieldCollection allOrderLineFields, XmlNode orderLineNode)
        {
            if (settings.AddOrderLineFieldsToRequest && allOrderLineFields != null && allOrderLineFields.Count > 0)
            {
                XmlNode orderLineFieldNode = null;
                foreach (OrderLineField olf in allOrderLineFields)
                {
                    orderLineFieldNode = orderLineNode.SelectSingleNode(
                        $"column [@columnName='{olf.SystemName}']");
                    if (orderLineFieldNode != null)
                    {
                        if (orderLine.OrderLineFieldValues == null)
                        {
                            orderLine.OrderLineFieldValues = new OrderLineFieldValueCollection();
                        }

                        OrderLineFieldValue olfv = orderLine.OrderLineFieldValues.FirstOrDefault(fv => fv != null && string.Compare(olf.SystemName, fv.OrderLineFieldSystemName, StringComparison.OrdinalIgnoreCase) == 0);
                        if (olfv != null)
                        {
                            olfv.Value = orderLineFieldNode.InnerText;
                        }
                        else
                        {
                            olfv = new OrderLineFieldValue(olf.SystemName, orderLineFieldNode.InnerText);
                            orderLine.OrderLineFieldValues.Add(olfv);
                        }
                    }
                }
            }
        }

        private static void SetTotalOrderDiscount(Order order)
        {
            PriceInfo totalDiscount = new PriceInfo(order.Currency); ;
            foreach (var line in order.OrderLines.Where(x => x.HasType(new[] { OrderLineType.Discount, OrderLineType.ProductDiscount })))
            {
                totalDiscount = totalDiscount.Add(line.Price);
            }
            order.TotalDiscount.PriceWithVAT = totalDiscount.PriceWithVAT;
            order.TotalDiscount.PriceWithoutVAT = totalDiscount.PriceWithoutVAT;
            order.TotalDiscount.VAT = totalDiscount.VAT;
        }

        #region Hash helper methods

        /// <summary>
        /// Gets the last order hash.
        /// </summary>
        /// <returns>System.String.</returns>
        private static string GetLastOrderHash(Settings settings)
        {
            string hashValue = null;
            if (Caching.Cache.Current != null)
            {
                object orderHash = Caching.Cache.Current[GetOrderHashCacheKey(settings)];

                if (orderHash is string)
                {
                    hashValue = orderHash as string;
                }
            }
            return hashValue;
        }

        /// <summary>
        /// Gets the cache key.
        /// </summary>
        /// <returns>System.String.</returns>
        private static string GetOrderHashCacheKey(Settings settings)
        {
            var user = Helpers.GetCurrentExtranetUser();
            string key = Constants.CacheConfiguration.OrderCommunicationHash;

            if (user != null)
            {
                key += user.ID.ToString();
            }
            else
            {
                key += settings.AnonymousUserKey;
            }

            return key;
        }

        /// <summary>
        /// Saves the order hash.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="hashValue">The hash value.</param>
        private static void SaveOrderHash(Settings settings, string hashValue)
        {
            if (Caching.Cache.Current != null)
            {
                Caching.Cache.Current[GetOrderHashCacheKey(settings)] = hashValue;
            }
        }

        #endregion Hash helper methods
    }
}