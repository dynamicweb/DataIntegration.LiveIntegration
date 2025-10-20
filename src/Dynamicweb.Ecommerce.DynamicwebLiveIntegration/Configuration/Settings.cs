using Dynamicweb.Core;
using Dynamicweb.Core.Helpers;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Cache;
using Dynamicweb.Extensibility.AddIns;
using Dynamicweb.Extensibility.Editors;
using Dynamicweb.Security.UserManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration
{
    /// <summary>
    /// Settings class
    /// </summary>
    /// <seealso cref="ISettings" />
    public class Settings : ISettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Settings" /> class.
        /// </summary>
        public Settings()
        {
            // default values
            InstanceId = string.Empty;
            IsLiveIntegrationEnabled = false;
            LazyLoadProductInfo = false;
            AddProductFieldsToRequest = true;

            QueueOrdersToExport = true;
            AddOrderFieldsToRequest = true;
            AddOrderLineFieldsToRequest = true;

            LiveProductInfoForAnonymousUsers = true;
            EnableCartCommunicationForAnonymousUsers = true;
            AnonymousUserKey = "Anonymous";
            ProductDiscountText = "Discount";
            OrderDiscountText = "Invoice discount";
            ErpShippingItemKey = "DW-SHIP";
            ErpShippingItemType = Constants.OrderConfiguration.DefaultShippingItemType;
            WebServiceConnectionStatusGlobalTagName = "Global:LiveIntegration.IsWebServiceConnectionAvailable";

            ErpControlsDiscount = true;
            ErpControlsShipping = true;

            SetOrderlineFixed = false;

            CalculatePriceUsingProductNumber = true;
            CalculateOrderUsingProductNumber = true;
            AutoPingInterval = Constants.DefaultPingInterval;
            ConnectionTimeout = Constants.DefaultConnectionTimeout;
            CartCommunicationType = Constants.CartCommunicationType.Full;            
        }

        /// <summary>
        /// Gets or sets the date and time when the last notification email was sent.
        /// </summary>
        /// <value>The last notification email sent.</value>
        public static DateTime LastNotificationEmailSent { get; set; }

        #region General parameters

        /// <summary>
        /// Instance Id
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// Instance Label
        /// </summary>
        public string InstanceLabel { get; set; }

        /// <summary>
        /// InstanceName
        /// </summary>
        [XmlIgnore]
        public string InstanceName
        {
            get
            {
                string prefix = !string.IsNullOrEmpty(InstanceId) ? $"_{InstanceId}" : string.Empty;
                return $"{Constants.AddInName}{prefix}";
            }
        }

        /// <summary>
        /// Settings file
        /// </summary>
        [XmlIgnore]
        public string SettingsFile { get; set; }

        /// <summary>
        /// Gets or sets the ERP connection timeout in seconds.
        /// </summary>
        /// <value>Returns the connection timeout.</value>
        public int ConnectionTimeout { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if live integration is enabled.
        /// </summary>
        /// <value>Returns <c>true</c> if live integration is enabled; otherwise, <c>false</c>.</value>
        public bool IsLiveIntegrationEnabled { get; set; }

        /// <summary>
        /// Gets or sets the security key used to communicate with the ERP service.
        /// </summary>
        /// <value>The security key.</value>
        public string SecurityKey { get; set; }

        /// <summary>
        /// Gets or sets the ID of the shop used in this integration.
        /// </summary>
        /// <value>The shop identifier.</value>
        public string ShopId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the website used in this integration.
        /// </summary>
        /// <value>The website identifier.</value>
        public int AreaId { get; set; }

        /// <summary>
        /// Global tag for connection availability
        /// </summary>
        /// <value>The name of the web service connection status global tag.</value>
        public string WebServiceConnectionStatusGlobalTagName { get; set; }

        /// <summary>
        /// Gets or sets the url for the Web Service.
        /// </summary>
        /// <value>The web service URI.</value>
        public string WebServiceURI { get; set; }

        /// <summary>
        /// Gets or sets the ids for the Endpoint.
        /// </summary>
        /// <value>The Endpoint ids.</value>
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the value for the ping interval.
        /// </summary>
        /// <value>The interval between pings (seconds).</value>
        public int AutoPingInterval { get; set; }

        #endregion General parameters

        #region Products parameters

        /// <summary>
        /// Gets or sets a value that determines if custom product fields are appended to the outgoing XML request.
        /// </summary>
        /// <value>Returns <c>true</c> if custom product fields should be added to the request; otherwise, <c>false</c>.</value>
        public bool AddProductFieldsToRequest { get; set; }

        /// <summary>
        /// Gets or sets a value that determines whether to use the product number in price calculations.
        /// </summary>
        /// <value><c>true</c> if [calculate price using product number]; otherwise, <c>false</c>.</value>
        public bool CalculatePriceUsingProductNumber { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if live prices are retrieved from the ERP.
        /// </summary>
        /// <value>Returns <c>true</c> if live prices should be retrieved from the ERP; otherwise, <c>false</c>.</value>
        public bool EnableLivePrices { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if product info should be lazy loaded.
        /// </summary>
        /// <value>Returns <c>true</c> if lazy loading for products is enabled; otherwise, <c>false</c>.</value>
        public bool LazyLoadProductInfo { get; set; }

        /// <summary>
        /// Gets or sets the cache level for products.
        /// </summary>
        /// <value>The product cache level.</value>
        public string ProductCacheLevel { get; set; }

        /// <summary>
        /// Gets or sets a value that determines the max number of products on each live request.
        /// </summary>
        public int MaxProductsPerRequest { get; set; }

        #endregion Products parameters

        #region Orders parameters

        /// <summary>
        /// Gets or sets a value that determines if custom order fields are appended to the outgoing XML request.
        /// </summary>
        /// <value>Returns <c>true</c> if order fields should be added to the request; otherwise, <c>false</c>.</value>
        public bool AddOrderFieldsToRequest { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if custom order line fields are appended to the outgoing XML request.
        /// </summary>
        /// <value>Returns <c>true</c> if order line fields should be added to the request; otherwise, <c>false</c>.</value>
        public bool AddOrderLineFieldsToRequest { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if parts information (BOM) is appended to the outgoing XML request.
        /// </summary>
        /// <value>Returns <c>true</c> if parts order lines should e added to the request; otherwise, <c>false</c>.</value>
        public bool AddOrderLinePartsToRequest { get; set; }

        /// <summary>
        /// Gets or sets the type of communication used for the cart. Valid options are defined in <see cref="Constants.CartCommunicationType" />.
        /// </summary>
        /// <value>Returns the type of the cart communication.</value>
        public string CartCommunicationType { get; set; }

        /// <summary>
        /// Gets or sets the cache level for order information.
        /// </summary>
        /// <value>The order cache level.</value>
        public string OrderCacheLevel { get; set; }

        /// <summary>
        /// Gets or sets the order state that is applied after order export failed.
        /// </summary>
        /// <value>The order state after export failed.</value>
        public string OrderStateAfterExportFailed { get; set; }

        /// <summary>
        /// Gets or sets the order state that is applied after order export succeeded.
        /// </summary>
        /// <value>The order state after export succeeded.</value>
        public string OrderStateAfterExportSucceeded { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if orders can be queued to be exported later.
        /// </summary>
        /// <value>Returns <c>true</c> if orders can be queued for later export; otherwise, <c>false</c>.</value>
        public bool QueueOrdersToExport { get; set; }

        /// <summary>
        /// Gets or sets a value that determines whether to skip sending ledger entries to the ERP.
        /// </summary>
        public bool SkipLedgerOrder { get; set; }

        /// <summary>
        /// Gets or sets the text for product discounts.
        /// </summary>
        /// <value>The text for product discounts.</value>
        public string ProductDiscountText { get; set; }

        /// <summary>
        /// Gets or sets the text for order discount order line.
        /// </summary>
        /// <value>The text for order discount order line.</value>
        public string OrderDiscountText { get; set; }

        /// <summary>
        /// Gets or sets if ERP controls discount calculations
        /// </summary>
        /// <value><c>true</c> if [ERP controls discount calculations]; otherwise, <c>false</c>.</value>
        public bool ErpControlsDiscount { get; set; }

        /// <summary>
        /// Gets or sets if ERP controls shipping
        /// </summary>
        /// <value><c>true</c> if [ERP controls shipping]; otherwise, <c>false</c>.</value>
        public bool ErpControlsShipping { get; set; }

        /// <summary>
        /// Gets or sets the key for shipping item type.
        /// </summary>
        /// <value>The key for shipping item type.</value>
        public string ErpShippingItemType { get; set; }

        /// <summary>
        /// Gets or sets the key for shipping item.
        /// </summary>
        /// <value>The key for shipping item.</value>
        public string ErpShippingItemKey { get; set; }

        /// <summary>
        /// Gets or sets if use unit prices
        /// </summary>
        /// <value><c>true</c> if [use unit prices]; otherwise, <c>false</c>.</value>
        public bool UseUnitPrices { get; set; }

        /// <summary>
        /// Gets or sets if the orderline should be set to fixed when unitprice is set by the orderhandler
        /// </summary>
        public bool SetOrderlineFixed { get; set; }

        /// <summary>
        /// Gets or sets a value that determines whether to use the product number in order calculations.
        /// </summary>
        /// <value><c>true</c> if [calculate order using product number]; otherwise, <c>false</c>.</value>
        public bool CalculateOrderUsingProductNumber { get; set; }

        #endregion Orders parameters

        #region Users parameters        

        /// <summary>
        /// Gets or sets the number format culture.
        /// </summary>
        /// <value>The number format culture.</value>
        public string NumberFormatCulture { get; set; }

        /// <summary>
        /// Gets or sets the customer name used in integration scenarios with anonymous users.
        /// </summary>
        /// <value>Returns the anonymous user key.</value>
        public string AnonymousUserKey { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if (live) cart communication is enabled for anonymous users.
        /// </summary>
        /// <value>Returns <c>true</c> if cart communication is enabled for anonymous users; otherwise, <c>false</c>.</value>
        public bool EnableCartCommunicationForAnonymousUsers { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if the product request should contain all product variants or only the product being requested.
        /// </summary>
        public bool GetProductInformationForAllVariants { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if live product information should be retrieved for anonymous users.
        /// </summary>
        /// <value>Returns <c>true</c> if live product information is enabled for anonymous users; otherwise, <c>false</c>.</value>
        public bool LiveProductInfoForAnonymousUsers { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if calls to the ERP for live product info should be retried in case of a failure.
        /// </summary>
        public bool MakeRetryForLiveProductInformation { get; set; }

        /// <summary>
        /// When enabled anonymous users will receive discounts calculated by DynamicWeb instead of retrieving them from the ERP via Live Integration
        /// </summary>
        /// <value><c>true</c> if [disable ERP discounts calculation for anonymous users]; otherwise, <c>false</c>.</value>        
        public bool DisableErpDiscountsForAnonymousUsers { get; set; }

        #endregion Users parameters

        #region Notifications parameters

        /// <summary>
        /// Gets or sets the recipient's email address for notification emails.
        /// </summary>
        /// <value>The notification email.</value>
        public string NotificationEmail { get; set; }

        /// <summary>
        /// Gets or sets the recipient's user groups for notification emails.
        /// </summary>
        /// <value>The comma separated user group ids.</value>
        public string RecipientGroups { get; set; }

        /// <summary>
        /// Gets or sets the sender's email address for notification emails.
        /// </summary>
        /// <value>The notification email sender email.</value>
        public string NotificationEmailSenderEmail { get; set; }

        /// <summary>
        /// Gets or sets the sender's name for notification emails.
        /// </summary>
        /// <value>The name of the notification email sender.</value>
        public string NotificationEmailSenderName { get; set; }

        /// <summary>
        /// Gets or sets the subject of the notification email.
        /// </summary>
        /// <value>The notification email subject.</value>
        public string NotificationEmailSubject { get; set; }

        /// <summary>
        /// Gets or sets the frequency at which notifications should be sent.
        /// </summary>
        /// <value>The notification sending frequency.</value>
        public string NotificationSendingFrequency { get; set; }

        /// <summary>
        /// Gets or sets the template used for the notification email.
        /// </summary>
        /// <value>The notification template.</value>
        public string NotificationTemplate { get; set; }

        #endregion Notifications parameters

        #region Logs parameters

        /// <summary>
        /// Gets or sets a value that indicates whether to keep log files or to truncate a single log file.
        /// </summary>
        public bool KeepLogFiles { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if connection errors should be logged.
        /// </summary>
        public bool LogConnectionErrors { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if debug info should be logged.
        /// </summary>
        public bool LogDebugInfo { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if general info should be logged.
        /// </summary>
        public bool LogGeneralErrors { get; set; }

        /// <summary>
        /// Gets or sets a value that determines the maximum file size for the log before roll-over or clipping is applied.
        /// </summary>
        /// <value>The maximum size of the log.</value>
        public int LogMaxSize { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if response errors should be logged.
        /// </summary>
        public bool LogResponseErrors { get; set; }

        /// <summary>
        /// Gets a value indicating whether to save a copy of the XML in the logs folder when a completed order is sent to the ERP.
        /// </summary>
        /// <value><c>true</c> if a copy should be saved; otherwise, <c>false</c>.</value>
        public bool SaveCopyOfOrderXml { get; set; }

        #endregion Logs parameters        

        /// <summary>
        /// Updates the target settings with the source values.
        /// </summary>
        /// <param name="source">The source to copy from.</param>
        /// <param name="target">The target to copy to.</param>
        public static void UpdateFrom(ISettings source, ISettings target)
        {
            if (source != null && target != null)
            {
                target.IsLiveIntegrationEnabled = source.IsLiveIntegrationEnabled;
                target.WebServiceURI = source.WebServiceURI;
                target.Endpoint = source.Endpoint;
                target.ShopId = source.ShopId;
                target.AreaId = source.AreaId;
                target.SecurityKey = source.SecurityKey;
                target.ConnectionTimeout = source.ConnectionTimeout;
                target.AutoPingInterval = source.AutoPingInterval < Constants.MinPingInterval ? Constants.MinPingInterval : source.AutoPingInterval;
                target.ProductDiscountText = source.ProductDiscountText;
                target.OrderDiscountText = source.OrderDiscountText;
                target.NumberFormatCulture = source.NumberFormatCulture;
                target.WebServiceConnectionStatusGlobalTagName = source.WebServiceConnectionStatusGlobalTagName;

                target.LiveProductInfoForAnonymousUsers = source.LiveProductInfoForAnonymousUsers;
                target.MakeRetryForLiveProductInformation = source.MakeRetryForLiveProductInformation;
                target.GetProductInformationForAllVariants = source.GetProductInformationForAllVariants;
                target.EnableCartCommunicationForAnonymousUsers = source.EnableCartCommunicationForAnonymousUsers;
                target.AnonymousUserKey = source.AnonymousUserKey;
                target.EnableLivePrices = source.EnableLivePrices;
                target.UseUnitPrices = source.UseUnitPrices;
                target.MaxProductsPerRequest = source.MaxProductsPerRequest;

                target.ProductCacheLevel = source.ProductCacheLevel;
                target.CalculatePriceUsingProductNumber = source.CalculatePriceUsingProductNumber;
                target.OrderCacheLevel = source.OrderCacheLevel;
                target.OrderStateAfterExportSucceeded = source.OrderStateAfterExportSucceeded;
                target.OrderStateAfterExportFailed = source.OrderStateAfterExportFailed;
                target.SkipLedgerOrder = source.SkipLedgerOrder;
                target.ErpControlsDiscount = source.ErpControlsDiscount;
                target.DisableErpDiscountsForAnonymousUsers = source.DisableErpDiscountsForAnonymousUsers;
                target.ErpControlsShipping = source.ErpControlsShipping;
                target.ErpShippingItemType = source.ErpShippingItemType;
                target.ErpShippingItemKey = source.ErpShippingItemKey;

                target.SetOrderlineFixed = source.SetOrderlineFixed;

                target.SaveCopyOfOrderXml = source.SaveCopyOfOrderXml;
                target.LogConnectionErrors = source.LogConnectionErrors;
                target.LogResponseErrors = source.LogResponseErrors;
                target.LogGeneralErrors = source.LogGeneralErrors;
                target.LogDebugInfo = source.LogDebugInfo;
                target.LogMaxSize = source.LogMaxSize;
                target.KeepLogFiles = source.KeepLogFiles;

                target.NotificationEmail = source.NotificationEmail;
                target.RecipientGroups = source.RecipientGroups;
                target.NotificationTemplate = source.NotificationTemplate;
                target.NotificationEmailSubject = source.NotificationEmailSubject;
                target.NotificationEmailSenderName = source.NotificationEmailSenderName;
                target.NotificationEmailSenderEmail = source.NotificationEmailSenderEmail;
                target.NotificationSendingFrequency = source.NotificationSendingFrequency;

                target.AddOrderFieldsToRequest = source.AddOrderFieldsToRequest;
                target.AddOrderLineFieldsToRequest = source.AddOrderLineFieldsToRequest;
                target.AddProductFieldsToRequest = source.AddProductFieldsToRequest;

                target.LazyLoadProductInfo = source.LazyLoadProductInfo;
                target.QueueOrdersToExport = source.QueueOrdersToExport;
                target.AddOrderLinePartsToRequest = source.AddOrderLinePartsToRequest;
                target.CartCommunicationType = source.CartCommunicationType;
                target.CalculateOrderUsingProductNumber = source.CalculateOrderUsingProductNumber;

                if (target.LogMaxSize == 0)
                {
                    target.LogMaxSize = 10; // MB
                }
                target.InstanceId = source.InstanceId;
                target.InstanceLabel = source.InstanceLabel;
                target.SettingsFile = source.SettingsFile;                
                if(source is Settings sourceSettings)
                {
                    sourceSettings._notificationRecipients = null;
                }
                if (target is Settings targetSettings)
                {
                    targetSettings._notificationRecipients = null;
                }
            }
        }

        /// <summary>
        /// Gets the product cache level.
        /// </summary>
        /// <value>The product cache level.</value>
        public ResponseCacheLevel GetProductCacheLevel() => Helpers.GetEnumValueFromString(ProductCacheLevel, ResponseCacheLevel.Page);

        private List<string> _notificationRecipients = null;
        internal List<string> NotificationRecipients
        {
            get
            {
                if (_notificationRecipients is null)
                {
                    _notificationRecipients = GetNotificationRecipients();
                }
                return _notificationRecipients;
            }
        }

        private List<string> GetNotificationRecipients()
        {            
            var recipients = new List<string>();
            if (!string.IsNullOrEmpty(NotificationEmail) && StringHelper.IsValidEmailAddress(NotificationEmail))
                recipients.Add(NotificationEmail);
            if (!string.IsNullOrEmpty(RecipientGroups))
            {               
                recipients.AddRange(GetRecipients(RecipientGroups.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(id => Converter.ToInt32(id))));
            }
            return recipients.Distinct().ToList();
        }

        private static List<string> GetRecipients(IEnumerable<int> groupIds)
        {
            List<string> result = [];
            var users = new List<User>();
            foreach (var groupId in groupIds)
            {
                var group = UserManagementServices.UserGroups.GetGroupById(groupId);
                if (group is null)
                    continue;
                foreach (var user in UserManagementServices.Users.GetUsersByGroupId(groupId))
                {
                    users.Add(user);
                }
                foreach (var subGroup in group.GetSubgroups())
                {
                    foreach (var user in subGroup.GetUsers())
                    {
                        if (!users.Contains(user))
                        {
                            users.Add(user);
                        }
                    }
                }
            }
            foreach (User user in users)
            {
                if (!user.Active || !StringHelper.IsValidEmailAddress(user.Email))
                {
                    continue;
                }
                result.Add(user.Email);
            }
            return result;
        }        
    }
}