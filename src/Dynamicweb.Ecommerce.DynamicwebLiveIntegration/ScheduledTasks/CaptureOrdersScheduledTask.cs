using Dynamicweb.Content.Files.Information;
using Dynamicweb.Ecommerce.Cart;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Ecommerce.Orders.Gateways;
using Dynamicweb.Extensibility.AddIns;
using Dynamicweb.Extensibility.Editors;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.ScheduledTasks
{
    [AddInName("LiveIntegrationCaptureOrders")]
    [AddInLabel("Capture orders using Live Integration")]
    [AddInDescription("Capture orders payments updated through Batch Integration and updating using Live Integration")]
    [AddInIgnore(false)]
    [AddInUseParameterGrouping(true)]
    public class CaptureOrdersScheduledTask : BatchIntegrationScheduledTaskAddinBase, IDropDownOptions
    {
        private readonly Dictionary<string, CheckoutHandler> _paymentHandlers = new Dictionary<string, CheckoutHandler>();
        private string _processError;
        private OrderInvoiceType _orderInvoiceTypeValue;
        private static string _leaveUnchangedValue = "Leave unchanged";

        private enum OrderInvoiceType
        {
            Invoice,
            Order,
            Both,
        }

        private enum CaptureResult
        {
            Success,
            Failure,
            NotComplete,
            AbortedDueToNoErpConnection,
            OrderNotCapturable,
            CaptureSucceededErpCommunicationFailed
        }

        #region Parameters
        #region A Type of filter
        #region Filters checked

        [AddInParameter("Field and Value options")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "NewUIcheckbox=true;")]
        [AddInParameterGroup("A) Type of filter")]
        public bool FilterByFieldAndValue { get; set; }

        [AddInParameter("Status")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "NewUIcheckbox=true;")]
        [AddInParameterGroup("A) Type of filter")]
        public bool FilterByStatus { get; set; }

        [AddInParameter("DoCapturePayment Boolean Flag")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "NewUIcheckbox=true;")]
        [AddInParameterGroup("A) Type of filter")]
        public bool FilterByDoCapturePaymentFlag { get; set; } = true;

        [AddInParameter("Single Order")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "NewUIcheckbox=true;")]
        [AddInParameterGroup("A) Type of filter")]
        public bool FilterBySingleOrder { get; set; }

        [AddInParameter("Shop")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "NewUIcheckbox=true;")]
        [AddInParameterGroup("A) Type of filter")]
        public bool FilterByShop { get; set; }
        #endregion

        [AddInParameter("Field")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "inputClass=NewUIinput;SortBy=Key;")]
        [AddInParameterGroup("A.1) Field and Value options")]
        public string FieldToSearch { get; set; }

        [AddInParameter("Value")]
        [AddInParameterEditor(typeof(TextParameterEditor), "inputClass=NewUIinput;")]
        [AddInParameterGroup("A.1) Field and Value options")]
        public string ValueToSearch { get; set; }

        [AddInParameter("Order States")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "NewGUI=true;none=false;multiple=true;SortBy=Key")]
        [AddInParameterGroup("A.2) Status filter")]
        public string StatusToSearch { get; set; }

        [AddInParameter("Order Id")]
        [AddInParameterEditor(typeof(TextParameterEditor), "inputClass=NewUIinput;")]
        [AddInParameterGroup("A.3) Single Order")]
        public string OrderId { get; set; }

        [AddInParameter("Shop Id")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "NewGUI=true;none=false;multiple=true;SortBy=Key")]
        [AddInParameterGroup("A.4) Shop")]
        public string ShopId { get; set; }
        #endregion

        #region B

        [AddInParameter("Order/Invoice Type")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "inputClass=NewUIinput;none=false;SortBy=Key;")]
        [AddInParameterGroup("B) Type of object")]
        public string OrderInvoiceTypeToProcess { get; set; } = OrderInvoiceType.Order.ToString();

        [AddInParameter("Update Invoice Payment Information")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "NewUIcheckbox=true;")]
        [AddInParameterGroup("B) Type of object")]
        public bool UpdateInvoicePaymentInformation { get; set; } = true;
        #endregion

        #region C

        [AddInParameter("Finished for X minutes")]
        [AddInParameterEditor(typeof(IntegerNumberParameterEditor), "NewUIcheckbox=true;Value=true;")]
        [AddInParameterGroup("C) Action")]
        public int MinutesCompleted { get; set; } = 5;

        [AddInParameter("Maximum orders per execution")]
        [AddInParameterEditor(typeof(IntegerNumberParameterEditor), "NewUIcheckbox=true;Value=true;")]
        [AddInParameterGroup("C) Action")]
        public int MaxOrdersToProcess { get; set; } = 25;

        #endregion

        #region D

        [AddInParameter("If success")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "NewGUI=true;none=false;SortBy=Key;")]
        [AddInDescription("Empty means leave unchanged")]
        [AddInParameterGroup("D) Order status")]
        public string SuccessOrderStateIdValue { get; set; }

        [AddInParameter("If error")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "NewGUI=true;none=false;SortBy=Key;")]
        [AddInDescription("Empty means leave unchanged")]
        [AddInParameterGroup("D) Order status")]
        public string ErrorOrderStateIdValue { get; set; }

        [AddInParameter("Update order to ERP after capture")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "NewUIcheckbox=true;")]
        [AddInParameterGroup("D) Order status")]
        public bool CommunicateBackToErp { get; set; } = true;

        [AddInParameter("Set PaymentCaptured=True even on failed orders")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "NewUIcheckbox=true;")]
        [AddInParameterGroup("D) Order status")]
        public bool SetPaymentCapturedOnFailedOrders { get; set; } = true;

        [AddInParameter("Include SQL Query in log for debugging")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "NewGUI=true;Value=true;")]
        [AddInParameterGroup("E) Debugging")]
        public bool IncludeSqlQueryInLogging { get; set; }

        #endregion
        #endregion

        /// <summary>
        /// Main method in ScheduledTask Addin - is run when scheduled Task is run
        /// </summary>
        /// <returns></returns>
        public override bool Run()
        {
            SetupLogging();

            bool processResult = true;
            _processError = "";

            var settings = GetSettings(ShopId);

            try
            {
                // read configuration
                _orderInvoiceTypeValue = GetOrderInvoiceTypeFromString();

                //confirm that order table has is custom flags to check payment status
                CheckOrderCustomFields();

                //get the relevant data from the DB
                var ordersToSync = ReadOrders();

                if (ordersToSync != null && ordersToSync.Count() > 0)
                {
                    if (CommunicateBackToErp && !Connector.IsWebServiceConnectionAvailable(settings))
                    {
                        var msg = "The ERP connection is currently unavailable, but 'Update order to ERP after capture' is enabled. Wait to retry when the ERP is back online.";
                        Logger.Log(msg);
                        _processError = msg;
                    }
                    else
                    {
                        ProcessOrders(ordersToSync);
                    }
                }
                else
                {
                    Logger.Log("No orders to be processed.");
                }
            }
            catch (Exception ex)
            {
                _processError = ex.Message;
                Logger.Log($"Error processing Capture job {ex.Message}\n {ex}");
            }
            finally
            {
                //handled errors during process.
                if (_processError != "")
                {
                    //Sendmail with error
                    processResult = false;
                    SendMailWithFrequency(_processError, settings, MessageType.Error);
                }
                else
                {
                    //Send mail with success 
                    SendMailWithFrequency("Scheduled task completed successfully", settings);
                }
                SetSuccessfulRun();
            }

            WriteTaskResultToLog(processResult);
            return processResult;
        }

        private OrderInvoiceType GetOrderInvoiceTypeFromString()
        {
            return !Enum.TryParse(OrderInvoiceTypeToProcess, out OrderInvoiceType result) ? OrderInvoiceType.Order : result;
        }

        private void CheckOrderCustomFields()
        {
            if (FilterByDoCapturePaymentFlag && !Services.OrderFields.GetOrderFields().Any(of => of.SystemName == "DoCapturePayment"))
            {
                var doCapturePayment = new OrderField
                {
                    SystemName = "DoCapturePayment",
                    Locked = true,
                    Name = "Make Capture Payment",
                    TypeId = 3,
                    TypeName = "checkbox"
                };

                Services.OrderFields.Save(doCapturePayment);
            }

            if (!Services.OrderFields.GetOrderFields().Any(of => of.SystemName == "PaymentCaptured"))
            {
                var paymentCaptured = new OrderField
                {
                    SystemName = "PaymentCaptured",
                    Locked = true,
                    Name = "Payment is done",
                    TypeId = 3,
                    TypeName = "checkbox"
                };

                Services.OrderFields.Save(paymentCaptured);
            }
        }

        private List<Order> ReadOrders()
        {
            OrderSearchFilter filter = new OrderSearchFilter
            {
                PageSize = MaxOrdersToProcess,
                ToDate = DateTime.Now.AddMinutes(-1 * MinutesCompleted),
                ShowUntransferred = true,
                ShowNotExported = true,
                Completed = OrderSearchFilter.CompletedStates.Completed
            };
            if (!string.IsNullOrEmpty(ShopId))
            {
                filter.ShopIds = new string[] { ShopId };
            }

            //for each optional filter add is condition to the sql
            ReadOrderFromCustomFields(filter);
            ReadOrderFromStatus(filter);
            ReadOrderFromOrderId(filter);
            ReadOrderFromShopId(filter);
            ReadOrderFromDoCapturePaymentFlag(filter);
            ReadOrderInvoiceType(filter);

            return new OrderService().GetOrdersBySearch(filter)?.GetResultOrders()?.ToList();
        }

        private void ReadOrderFromCustomFields(OrderSearchFilter filter)
        {
            if (FilterByFieldAndValue && !string.IsNullOrEmpty(FieldToSearch))
            {
                filter.SearchInCustomOrderFields = true;
                filter.TextSearch = ValueToSearch;
                //return $"	AND {FieldToSearch} = '{ValueToSearch.Replace("'", "''")}'\n";
            }            
        }

        private void ReadOrderFromStatus(OrderSearchFilter filter)
        {
            if (FilterByStatus && !string.IsNullOrEmpty(StatusToSearch))
            {
                filter.OrderStateIds = StatusToSearch.Split(',', StringSplitOptions.RemoveEmptyEntries);
            }           
        }

        private void ReadOrderFromOrderId(OrderSearchFilter filter)
        {
            if (FilterBySingleOrder && !string.IsNullOrEmpty(OrderId))
            {
                filter.OrderIds = new string[] { OrderId };
            }            
        }

        private void ReadOrderFromShopId(OrderSearchFilter filter)
        {
            if (FilterByShop && !string.IsNullOrEmpty(ShopId))
            {
                filter.SelectedShopId = ShopId;
            }         
        }

        private void ReadOrderFromDoCapturePaymentFlag(OrderSearchFilter filter)
        {
            //todo
            //return FilterByDoCapturePaymentFlag ? "	 And IsNull (DoCapturePayment, 0) = 1\n" : "";
        }

        private void ReadOrderInvoiceType(OrderSearchFilter filter)
        {
            switch (_orderInvoiceTypeValue)
            {
                case OrderInvoiceType.Invoice:
                    filter.IsLedgerEntries = true;                    
                    break;
                case OrderInvoiceType.Order:
                    filter.IsLedgerEntries = false;
                    break;                
            }
        }

        private void ProcessOrders(List<Order> ordersToSync)
        {
            Logger.Log($"The list of order to run: {string.Join(",", ordersToSync.Select(order => order.Id))}");
            Settings shopSettings = SettingsManager.GetSettingsByShop(ShopId);

            foreach (var order in ordersToSync)
            {
                Settings orderSettings = shopSettings;
                if (orderSettings == null)
                {
                    orderSettings = SettingsManager.GetSettingsByShop(order.ShopId);
                }
                if (orderSettings != null && orderSettings.IsLiveIntegrationEnabled)
                {
                    ProcessOrder(orderSettings, order);
                    Logger.Log($"Order Id = {order.Id}, has been processed!");
                }
                else
                {
                    Logger.Log($"Can not find Live integration setup for ShopId: {ShopId}");
                }
            }

            Logger.Log("All orders have been processed!");
        }

        private void ProcessOrder(Settings settings, Order order)
        {
            string newOrderState = "";
            string currentOrderId = order.Id;
            var captureResult = CaptureResult.NotComplete;

            try
            {
                // lookup payment info from original order
                if (UpdateInvoicePaymentInformation && order.IsLedgerEntry)
                {
                    // if unable to get payment details from order, then skip invoice
                    if (!SetOrderPaymentInfoOnInvoice(order))
                    {
                        return;
                    }
                }

                var ch = CheckPaymentMethod(order);
                if (ch is IRemoteCapture)
                {
                    //TODO - we should check if the config is set to Authorization or not
                    //we should do this only once per gateway (cache first request)
                    //we may also need to check for config diffences between the time the order was placed and the time the scheduled task runs

                    captureResult = MakeCapture(order);
                }
                else
                {
                    captureResult = CaptureResult.OrderNotCapturable;
                    Logger.Log($"Order ID {currentOrderId} not capturable! Marked as paid!");
                }

            }
            catch (Exception ex)
            {
                string message = $"Error in Capture. Order ID = {currentOrderId}, Exception = {ex}";
                _processError += message;
                Logger.Log(message);
            }
            finally
            {
                if (CommunicateBackToErp && captureResult == CaptureResult.Success)
                {
                    //write capture update to ERP. Don't update the OrderStateId based on ERP response. It's handled further down.
                    var result = OrderHandler.UpdateOrder(settings, order, SubmitType.CaptureTask, order.StateId, order.StateId);
                    if (result == null || !result.Value)
                    {
                        Logger.Log($"Order ID {currentOrderId} failed updating ERP. It's recommended to manually review this payment capture entry in ERP.\n");
                        captureResult = CaptureResult.CaptureSucceededErpCommunicationFailed;
                    }
                }

                var paid = order.OrderFieldValues.First(of => of.OrderField.SystemName == "PaymentCaptured");

                //Two things to consider: Whether it's marked as Paid so that it doesn't retry, and what the OrderStateId is.
                switch (captureResult)
                {
                    case CaptureResult.Success:
                        paid.Value = true;
                        newOrderState = SuccessOrderStateIdValue;
                        break;

                    case CaptureResult.Failure:
                        if (SetPaymentCapturedOnFailedOrders)
                            paid.Value = true;
                        newOrderState = ErrorOrderStateIdValue;
                        break;

                    case CaptureResult.OrderNotCapturable:
                        newOrderState = SuccessOrderStateIdValue; //judgment call on this. Could be reconsidered. 
                        paid.Value = true; //set to paid so that it doesn't retry, even if SetPaymentCapturedOnFailedOrders set to true
                        break;

                    case CaptureResult.AbortedDueToNoErpConnection:
                        newOrderState = order.StateId; //leave alone and set up for retry
                        paid.Value = false;
                        break;

                    case CaptureResult.CaptureSucceededErpCommunicationFailed: //depend on email to let user know that it couldn't update the Erp
                        paid.Value = true;
                        newOrderState = SuccessOrderStateIdValue; //judgment call on this too. Since capture itself is successful, use success status.
                        break;

                    default:
                        break;
                }

                UpdateStateAndSaveOrder(order, newOrderState);
            }
        }

        private CaptureResult MakeCapture(Order order)
        {
            string message;
            var result = Services.Orders.Capture(order);

            if (result == null)
            {
                message = $"Order ID {order.Id} Captured executed, Null returned!";
                _processError += message;
                Logger.Log(message);

                return CaptureResult.AbortedDueToNoErpConnection;
            }
            else if (result.State != OrderCaptureInfo.OrderCaptureState.Success &&
                        result.State != OrderCaptureInfo.OrderCaptureState.Split)
            {
                message = $"Order ID {order.Id} Captured with result message: {result.Message} result state: {result.State}";
                _processError += message;
                Logger.Log(message);
                return CaptureResult.Failure;
            }
            else
            {
                Logger.Log($"Order ID {order.Id} Successfully Captured with result: {result.Message}");
                return CaptureResult.Success;
            }
        }

        private CheckoutHandler CheckPaymentMethod(Order order)
        {
            CheckoutHandler ch;
            var payId = order.PaymentMethodId;

            if (string.IsNullOrEmpty(payId))
            {
                return null;
            }

            if (_paymentHandlers.ContainsKey(payId))
            {
                ch = _paymentHandlers[payId];
            }
            else
            {
                ch = CheckoutHandler.GetCheckoutHandlerFromPaymentID(payId);
                _paymentHandlers.Add(payId, ch);
            }

            return ch;
        }

        private static void UpdateStateAndSaveOrder(Order order, string state)
        {
            if (!string.IsNullOrWhiteSpace(state) && state != _leaveUnchangedValue)
            {
                order.StateId = state;
            }

            Services.Orders.Save(order);
        }

        private bool SetOrderPaymentInfoOnInvoice(Order invoice)
        {
            var order = Services.Orders.GetById(invoice.IntegrationOrderId);
            if (order == null)
            {
                var message = $"Error in Capture. Invoice with ID {invoice.Id} does not match an Order on [Invoice].OrderIntegrationOrderID = [Order].OrderID. No action taken.\n";
                _processError += message;
                Logger.Log(message);
                return false;
            }
            invoice.PaymentMethodId = order.PaymentMethodId;
            invoice.GatewayResult = order.GatewayResult;
            invoice.TransactionNumber = order.TransactionNumber;
            invoice.TransactionCardNumber = order.TransactionCardNumber;
            invoice.TransactionCardType = order.TransactionCardType;
            invoice.TransactionPayGatewayCode = order.TransactionPayGatewayCode;
            invoice.CaptureAmount = order.CaptureAmount;
            invoice.CaptureInfo = order.CaptureInfo;
            Services.Orders.UpdateGatewayResult(invoice, false);
            return true;
        }

        /// <summary>
        /// gets options for Import Activity Dropdown
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Hashtable GetOptions(string name)
        {
            var options = new Hashtable();
            switch (name)
            {
                case "Field":
                    foreach (var field in Services.OrderFields.GetOrderFields())
                    {
                        options.Add(field.SystemName, field.Name);
                    }
                    break;
                case "If success":
                case "If error":
                    options.Add("", _leaveUnchangedValue);
                    goto case "Order States";
                case "Order States":
                    foreach (OrderState state in Services.OrderStates.GetStatesByOrderType(OrderType.Order))
                    {
                        if (state.IsDeleted)
                            continue;
                        options.Add(state.Id, Helpers.GetStateLabel(state));
                    }
                    break;
                case "Order/Invoice Type":
                    foreach (var invoiceType in Enum.GetValues(typeof(OrderInvoiceType)))
                    {
                        options.Add(invoiceType, Enum.GetName(typeof(OrderInvoiceType), invoiceType));
                    }
                    break;
                case "Shop Id":
                    options.Add("", "Any");

                    foreach (var shop in Services.Shops.GetShops())
                    {
                        options.Add(shop.Id, shop.Name);
                    }
                    break;
            }
            return options;
        }
    }
}
