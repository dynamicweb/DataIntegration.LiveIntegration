using Dynamicweb.Content.Files.Information;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Extensibility.AddIns;
using Dynamicweb.Extensibility.Editors;
using Dynamicweb.SystemTools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.ScheduledTasks
{
    [AddInName("LiveIntegrationUpdateOrders")]
    [AddInLabel("Update orders based on custom fields")]
    [AddInDescription("Update orders based on custom fields to update order state and this will generate emails notifications.")]
    [AddInIgnore(false)]
    [AddInUseParameterGrouping(true)]
    public class UpdateOrdersScheduledTask : BatchIntegrationScheduledTaskAddinBase, IDropDownOptions
    {
        private string _processErrorForEmail;

        public UpdateOrdersScheduledTask()
        {
            // default values
            MaxOrdersToProcess = 25;
            MinutesCompleted = 5;
            UpdateCustomField = true;
            NewState = "";
            FilterState = "";
            ForceStateUpdate = false;
            NotificationEmailFailureOnly = true;
            SkipCustomFieldUpdateOnErpFailure = false;
        }

        #region Parameters
        #region A) General

        [AddInParameter("Finished for X minutes")]
        [AddInParameterEditor(typeof(IntegerNumberParameterEditor), "allowNegativeValues=false;NewGUI=true;")]
        [AddInParameterGroup("A) General Filter")]
        public int MinutesCompleted { get; set; }

        [AddInParameter("Maximum orders per execution")]
        [AddInParameterEditor(typeof(IntegerNumberParameterEditor), "allowNegativeValues=false;NewGUI=true;")]
        [AddInParameterGroup("A) General Filter")]
        public int MaxOrdersToProcess { get; set; }

        [AddInParameter("State Filter")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "NewGUI=true;multiple=true;SortBy=Key;height=160;width=250;")]
        [AddInParameterGroup("A) General Filter")]
        public string FilterState { get; set; }
        #endregion

        #region B) Fields to compare

        [AddInParameter("Field A")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "NewGUI=true;multiple=false;SortBy=Key")]
        [AddInParameterGroup("B) Fields to compare")]
        public string FieldA { get; set; }

        [AddInParameter("Default value for NULL")]
        [AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true;")]
        [AddInParameterGroup("B) Fields to compare")]
        public string DefaultForNull { get; set; }

        [AddInParameter("Value to compare (not equal)")]
        [AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true;infoText=This option takes precedence over Field B")]
        [AddInParameterGroup("B) Fields to compare")]
        public string ValueToCompare { get; set; }

        [AddInParameter("Field B")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "NewGUI=true;multiple=false;SortBy=Key")]
        [AddInParameterGroup("B) Fields to compare")]
        public string FieldB { get; set; }
        #endregion

        #region C) Action on update

        [AddInParameter("Update custom fields (Field B is replaced by Field A)")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "NewGUI=false")]
        [AddInParameterGroup("C) Action on update")]
        public bool UpdateCustomField { get; set; }

        [AddInParameter("Order State")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "NewGUI=true;none=false;multiple=false;SortBy=Key")]
        [AddInParameterGroup("C) Action on update")]
        public string NewState { get; set; }

        [AddInParameter("Force State update")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "NewGUI=false")]
        [AddInParameterGroup("C) Action on update")]
        public bool ForceStateUpdate { get; set; }

        [AddInParameter("Value to update (success update => field A gets this value)")]
        [AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true;")]
        [AddInParameterGroup("C) Action on update")]
        public string ValueToUpdate { get; set; }

        [AddInParameter("Skip Custom Field Update on ERP Failure")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "NewGUI=false")]
        [AddInParameterGroup("C) Action on update")]
        public bool SkipCustomFieldUpdateOnErpFailure { get; set; }
        #endregion

        #region D) Notification

        [AddInParameter("Subject")]
        [AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true;")]
        [AddInParameterGroup("D) Notification")]
        public string Subject { get; set; }

        [AddInParameter("Sender Name")]
        [AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true;")]
        [AddInParameterGroup("D) Notification")]
        public string SenderName { get; set; }

        [AddInParameter("Sender e-mail")]
        [AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true;")]
        [AddInParameterGroup("D) Notification")]
        public string SenderEmail { get; set; }

        [AddInParameter("Send to")]
        [AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true;")]
        [AddInParameterGroup("D) Notification")]
        public string SendTo { get; set; }

        [AddInParameter("Send email to customer")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "NewGUI=false;")]
        [AddInParameterGroup("D) Notification")]
        public bool SendToCustomer { get; set; }

        [AddInParameter("Email template")]
        [AddInParameterEditor(typeof(TemplateParameterEditor), "NewGUI=true;")]
        [AddInParameterGroup("D) Notification")]
        public string EmailTemplate { get; set; }
        #endregion

        #region E) Admin Notifications
        [AddInParameter("Log all requests and responses")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "NewGUI=false;")]
        [AddInParameterGroup("E) Admin Notifications")]
        public new bool LogRequestAndResponse { get; set; }

        [AddInParameter("Notification recipient e-mail")]
        [AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true;")]
        [AddInParameterGroup("E) Admin Notifications")]
        public new string NotificationEmail { get; set; }

        [AddInParameter("On failure only")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "NewGUI=false;")]
        [AddInParameterGroup("E) Admin Notifications")]
        public new bool NotificationEmailFailureOnly { get; set; }

        [AddInParameter("Notification sender e - mail")]
        [AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true;")]
        [AddInParameterGroup("E) Admin Notifications")]
        public new string NotificationEmailSenderEmail { get; set; }

        [AddInParameter("Notification e-mail sender name")]
        [AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true;")]
        [AddInParameterGroup("E) Admin Notifications")]
        public new string NotificationEmailSenderName { get; set; }

        [AddInParameter("Notification e-mail subject")]
        [AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true;")]
        [AddInParameterGroup("E) Admin Notifications")]
        public new string NotificationEmailSubject { get; set; }

        [AddInParameter("Notification e-mail template")]
        [AddInParameterEditor(typeof(TextParameterEditor), "NewGUI=true;")]
        [AddInParameterGroup("E) Admin Notifications")]
        public new string NotificationTemplate { get; set; }
        #endregion
        #endregion

        /// <summary>
        /// Main method in ScheduledTask Addin - is run when scheduled Task is run
        /// </summary>
        /// <exception cref="ArgumentException">When Field A property or Field B property is not selected</exception>
        /// <returns></returns>
        public override bool Run()
        {
            SetupLogging();

            bool processResult = true;
            _processErrorForEmail = "";

            var settings = GetSettings();

            try
            {
                // validate parameters
                if (string.IsNullOrEmpty(FieldA))
                {
                    throw new ArgumentException("Invalid field parameter!", "Field A");
                }

                if (string.IsNullOrEmpty(FieldB) && string.IsNullOrEmpty(ValueToCompare))
                {
                    throw new Exception("Invalid parameters setup!");
                }

                // get the relevant data from the DB
                var ordersToSync = ReadOrders();

                if (ordersToSync != null && ordersToSync.Count() > 0)
                {
                    ProcessOrders(ordersToSync);
                }
                else
                {
                    Logger.Log("No orders to be processed.");
                }
            }
            catch (Exception ex)
            {
                _processErrorForEmail = ex.Message;
                Logger.Log($"Error processing Update Orders job {ex.Message}\n {ex}");
            }
            finally
            {                
                // handled errors during process.
                if (_processErrorForEmail != "")
                {
                    // Sendmail with error
                    processResult = false;
                    SendMailWithFrequency(_processErrorForEmail, settings, MessageType.Error);
                }
                else
                {
                    // Send mail with success 
                    SendMailWithFrequency(Translator.Translate("Scheduled task completed successfully"), settings);
                }
                SetSuccessfulRun();
            }

            WriteTaskResultToLog(processResult);
            return processResult;
        }

        private List<Order> ReadOrders()
        {
            var sbSql = new System.Text.StringBuilder();            
            var fieldTypeId = Common.Application.OrderFields.FirstOrDefault(of => of?.SystemName == FieldA)?.TypeId;
            var valueToCompare = string.IsNullOrEmpty(ValueToCompare) ? FieldB : GetCustomFieldSqlValue(fieldTypeId, ValueToCompare);

            sbSql.AppendFormat(
          @"SELECT top {0} * 
          FROM EcomOrders 
          WHERE OrderComplete = 1 AND OrderDeleted = 0 
	          and OrderCompletedDate < DATEADD(MINUTE, -{1}, GETDATE())
	          and IsNull ({2}, {4}) != {3}"
                , MaxOrdersToProcess, MinutesCompleted, FieldA, valueToCompare, DefaultForNull);

            if (!string.IsNullOrEmpty(FilterState))
            {
                sbSql.AppendFormat("\n	AND OrderStateID in ('{0}')", FilterState.Replace(",", "','"));
            }

            return Order.GetOrders(sbSql.ToString(), true).ToList();
        }

        private void ProcessOrders(List<Order> ordersToSync)
        {
            Logger.Log($"The list of order to run: {string.Join(",", ordersToSync.Select(x => x.Id))}");

            foreach (var order in ordersToSync)
            {
                var erpUpdated = SendToErp(order);
                if (!SkipCustomFieldUpdateOnErpFailure || erpUpdated)
                    UpdateOrderCustomField(order);
            }

            Logger.Log("All orders done!");
        }

        private void UpdateOrderCustomField(Order order)
        {
            try
            {
                // update custom field (if wanted)
                if (UpdateCustomField)
                {
                    var fieldA = order.OrderFieldValues.First(of => of.OrderField.SystemName == FieldA);
                    var fieldB = order.OrderFieldValues.First(of => of.OrderField.SystemName == FieldB);

                    fieldB.Value = fieldA.Value;
                }
                else if (!string.IsNullOrEmpty(ValueToUpdate))
                {
                    var fieldA = order.OrderFieldValues.First(of => of.OrderField.SystemName == FieldA);
                    fieldA.Value = ValueToUpdate;
                }

                // update order state
                if (!string.IsNullOrWhiteSpace(NewState))
                {
                    // check if it's needed to force the change state
                    if (order.StateId == NewState && ForceStateUpdate)
                    {
                        order.StateId = "";
                    }

                    order.StateId = NewState;
                }

                // save changes to fire notifications
                Services.Orders.Save(order);

                // send new notification from batch parameters
                SendNotification(order);

                Logger.Log($"Order Id {order.Id} done!");
            }
            catch (Exception ex)
            {
                var msg = $"Error in Update. Order Id = {order.Id}, Exception = {ex}";
                _processErrorForEmail += $"{msg}\n";
                Logger.Log(msg);
            }
        }

        private bool SendToErp(Order order)
        {
            try
            {
                var settings = GetSettings(order.ShopId);                
                // save capture changes and try to update ERP
                if (settings != null && settings.IsLiveIntegrationEnabled && Connector.IsWebServiceConnectionAvailable(settings))
                {
                    var result = OrderHandler.UpdateOrder(settings, order, SubmitType.ScheduledTask);
                    if (result == null || !result.Value)
                    {
                        _processErrorForEmail += $"Order Id {order.Id} was not updated to the ERP.\n";
                        Logger.Log($"Order Id {order.Id} was not updated to the ERP.");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                var msg = $"Failure trying to update the ERP for Order Id {order.Id}: {e}";
                _processErrorForEmail += $"{msg}\n";
                Logger.Log(msg);
                return false;
            }
            return true;
        }

        private void SendNotification(Order order)
        {
            // confirm that settings are valid to send
            if (string.IsNullOrEmpty(Subject) || string.IsNullOrEmpty(SenderEmail) || string.IsNullOrEmpty(SendTo) && !SendToCustomer)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(SenderName))
            {
                SenderName = null;
            }

            // prepare data to send
            var slTo = new List<string>();
            var page = Dynamicweb.Frontend.PageView.GetPageviewByPageID(order.CheckoutPageId);
            var template = new Rendering.Template(EmailTemplate.Replace("Templates/", ""));

            if (!string.IsNullOrEmpty(SendTo))
            {
                slTo.AddRange(SendTo.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            }
            if (SendToCustomer)
            {
                slTo.Add(order.CustomerEmail);
            }

            Services.Orders.SendEmail(order, ref page, Subject, SendTo, SenderEmail, SenderName, ref template);
        }

        /// <summary>
        /// Gets options for the various drop down lists in this addin.
        /// </summary>
        /// <param name="name">The label of the control for which the options should be returned.</param>
        /// <returns></returns>
        public Hashtable GetOptions(string name)
        {
            var options = new Hashtable();
            switch (name)
            {
                case "Field A":
                case "Field B":                    
                    foreach (var field in Common.Application.OrderFields.Where(of => of != null))
                    {
                        if (!options.ContainsKey(field.SystemName))
                            options.Add(field.SystemName, field.Name);
                    }
                    break;
                case "Order State":
                    options.Add("", "Leave unchanged");
                    goto case "State Filter";
                case "State Filter":
                    foreach (var state in OrderState.GetAllOrderStates())
                    {
                        if (!options.ContainsKey(state.Id))
                        {
                            options.Add(state.Id, Helpers.GetStateLabel(state));
                        }
                    }
                    break;
            }
            return options;
        }

        private string GetCustomFieldSqlValue(int? fieldTypeId, string value)
        {            
            int type = fieldTypeId != null ? fieldTypeId.Value : 0;
            string result = value;
            switch (type)
            {
                case 6://"integer
                case 7://"double
                    return result;
                case 3: //"checkbox"
                    result = Core.Converter.ToBoolean(value) ? "1" : "0";
                    break;
                case 4: //"date"
                case 5: // "datetime"
                    result = Core.Converter.ToDateTime(value).ToString("yyyy-MM-dd HH:mm:ss.fff");
                    break;
                default:
                    result = $"'{value}'";
                    break;
            }
            return result;
        }
    }
}
