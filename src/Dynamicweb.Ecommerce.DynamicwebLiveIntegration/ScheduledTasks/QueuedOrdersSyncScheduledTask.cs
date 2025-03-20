using Dynamicweb.Content.Files.Information;
using Dynamicweb.DataIntegration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Extensibility.AddIns;
using Dynamicweb.Extensibility.Editors;
using Dynamicweb.SystemTools;
using System;
using System.Collections;
using System.Linq;
using static Dynamicweb.Ecommerce.Notifications.Ecommerce.Order;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.ScheduledTasks
{
    /// <summary>
    /// Class QueuedOrdersSyncScheduledTask.
    /// </summary>
    /// <seealso cref="BatchIntegrationScheduledTaskAddin" />
    /// <seealso cref="IDropDownOptions" />
    [AddInName("DynamicwebLiveIntegrationQueuedOrdersSync")]
    [AddInLabel("Sync queued orders using Dynamicweb Live Integration")]
    [AddInDescription("Sync queued orders using Dynamicweb Live Integration")]
    [AddInIgnore(false)]
    [AddInUseParameterGrouping(true)]
    public class QueuedOrdersSyncScheduledTask : BatchIntegrationScheduledTaskAddin, IDropDownOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueuedOrdersSyncScheduledTask"/> class.
        /// </summary>
        public QueuedOrdersSyncScheduledTask()
        {
            MinutesCompleted = 5;
            MaxOrdersToProcess = 25;
            ShopId = string.Empty;
            ExcludeRecurrent = true;
        }

        #region Parameters

        /// <summary>
        /// Gets or sets the minutes completed.
        /// </summary>
        /// <value>The minutes completed.</value>
        [AddInParameter("Finished for X minutes")]
        [AddInParameterEditor(typeof(IntegerNumberParameterEditor), "NewUIcheckbox=true;Value=true;")]
        [AddInParameterGroup("A) Queued Orders")]
        public int MinutesCompleted { get; set; }

        /// <summary>
        /// Gets or sets the maximum orders to process.
        /// </summary>
        /// <value>The maximum orders to process.</value>
        [AddInParameter("Maximum orders to process in each execution")]
        [AddInParameterEditor(typeof(IntegerNumberParameterEditor), "NewUIcheckbox=true;Value=true;")]
        [AddInParameterGroup("A) Queued Orders")]
        public int MaxOrdersToProcess { get; set; }

        /// <summary>
        /// Gets or sets the shop identifier.
        /// </summary>
        /// <value>The shop identifier.</value>
        [AddInParameter("Shop")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "NewGUI=true;none=false")]
        [AddInParameterGroup("A) Queued Orders")]
        public string ShopId { get; set; }

        /// <summary>
        /// Gets or sets the order states.
        /// </summary>
        /// <value>The order states.</value>
        [AddInParameter("Order States")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "NewGUI=true;none=false;multiple=true")]
        [AddInParameterGroup("A) Queued Orders")]
        public string OrderStates { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [exclude recurrent].
        /// </summary>
        /// <value><c>true</c> if [exclude recurrent]; otherwise, <c>false</c>.</value>
        [AddInParameter("Exclude recurring order templates")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "NewUIcheckbox=true;Value=true;")]
        [AddInParameterGroup("A) Queued Orders")]
        public bool ExcludeRecurrent { get; set; }

        /// <summary>
        /// Gets or sets if skip ledgers
        /// </summary>
        [AddInParameter("Exclude ledger orders")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "NewGUI=true;none=false;")]
        [AddInParameterGroup("A) Queued Orders")]
        public bool SkipLedgerOrder { get; set; }

        #endregion Parameters

        /// <summary>
        /// Main method in ScheduledTask Addin - is run when scheduled Task is run
        /// </summary>
        /// <returns><c>true</c> if task run was successful, <c>false</c> otherwise.</returns>
        public override bool Run()
        {
            SetupLogging();

            bool result = false;
            string error = string.Empty;

            try
            {
                OrderSearchFilter filter = new OrderSearchFilter
                {
                    PageSize = MaxOrdersToProcess,
                    ShowUntransferred = true,
                    ShowNotExported = true,
                    Completed = OrderSearchFilter.CompletedStates.Completed,
                    IncludeRecurringOrders = !ExcludeRecurrent
                };

                if (MinutesCompleted > 0)
                {
                    filter.ToCompletedDate = DateTime.Now.AddMinutes(-1 * MinutesCompleted);
                }
                if (!string.IsNullOrEmpty(ShopId))
                {
                    filter.ShopIds = new string[] { ShopId };
                }
                if (!string.IsNullOrEmpty(OrderStates))
                {
                    filter.OrderStateIds = OrderStates.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                }
                if (SkipLedgerOrder)
                {
                    filter.IsLedgerEntries = false;
                }
                else
                {
                    filter.IncludeLedgerEntries = true;
                }

                var ordersToSync = Services.Orders.GetOrdersBySearch(filter);
                
                if (ordersToSync != null && ordersToSync.GetResultOrders() != null && ordersToSync.GetResultOrders().Any())
                {
                    Settings shopSettings = SettingsManager.GetSettingsByShop(ShopId);
                    foreach (var order in ordersToSync.GetResultOrders())
                    {
                        Settings settings = shopSettings;
                        if (settings == null)
                        {
                            settings = SettingsManager.GetSettingsByShop(order.ShopId);
                        }
                        if (settings != null)
                        {
                            OrderHandler.UpdateOrder(settings, order, SubmitType.ScheduledTask);
                        }
                    }
                }

                result = true;
            }
            catch (Exception e)
            {
                error = e.Message;
                Logger.Log($"Error occured during execution. {error}");
            }
            finally
            {
                if (!string.IsNullOrEmpty(error))
                {
                    // Send email with error
                    SendMail(error, MessageType.Error);
                }
                else
                {
                    // Send mail with success
                    SendMail("Scheduled task completed successfully");
                }
            }

            WriteTaskResultToLog(result);

            return result;
        }

        /// <summary>
        /// Get options for editors with multiple values
        /// </summary>
        /// <param name="dropdownName">Name of the dropdown.</param>
        /// <returns>Hashtable.</returns>
        public Hashtable GetOptions(string dropdownName)
        {
            var options = new Hashtable();
            switch (dropdownName)
            {
                case "Shop":
                    options.Add(string.Empty, "Any");
                    var shops = Services.Shops.GetShops();
                    foreach (var shop in shops)
                    {
                        options.Add(shop.Id, shop.Name);
                    }

                    break;

                case "Order States":
                    foreach (OrderState state in Services.OrderStates.GetStatesByOrderType(OrderType.Order))
                    {
                        if (state.IsDeleted)
                            continue;
                        options.Add(state.Id, state.GetName(Services.Languages.GetDefaultLanguageId()));
                    }

                    break;
            }

            return options;
        }
    }
}