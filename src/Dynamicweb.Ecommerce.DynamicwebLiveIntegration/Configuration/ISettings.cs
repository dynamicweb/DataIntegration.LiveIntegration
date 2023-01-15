namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration
{
    /// <summary>
    /// Interface to define settings for the Live Integration.
    /// </summary>
    public interface ISettings
    {
        /// <summary>
        /// Instance Label
        /// </summary>
        string InstanceLabel { get; set; }

        /// <summary>
        /// Instance Id
        /// </summary>
        string InstanceId { get; set; }

        /// <summary>
        /// Settings file
        /// </summary>
        string SettingsFile { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if live integration is enabled.
        /// </summary>
        /// <value>Returns <c>true</c> if live integration is enabled; otherwise, <c>false</c>.</value>
        bool IsLiveIntegrationEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if custom order fields are appended to the outgoing XML request.
        /// </summary>
        /// <value>Returns <c>true</c> if order fields should be added to the request; otherwise, <c>false</c>.</value>
        bool AddOrderFieldsToRequest { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if custom order line fields are appended to the outgoing XML request.
        /// </summary>
        /// <value>Returns <c>true</c> if order line fields should be added to the request; otherwise, <c>false</c>.</value>
        bool AddOrderLineFieldsToRequest { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if parts information (BOM) is appended to the outgoing XML request.
        /// </summary>
        /// <value>Returns <c>true</c> if parts order lines should e added to the request; otherwise, <c>false</c>.</value>
        bool AddOrderLinePartsToRequest { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if custom product fields are appended to the outgoing XML request.
        /// </summary>
        /// <value>Returns <c>true</c> if custom product fields should be added to the request; otherwise, <c>false</c>.</value>
        bool AddProductFieldsToRequest { get; set; }

        /// <summary>
        /// Gets or sets the customer name used in integration scenarios with anonymous users.
        /// </summary>
        /// <value>Returns the anonymous user key.</value>
        string AnonymousUserKey { get; set; }

        /// <summary>
        /// Gets or sets the type of communication used for the cart. Valid options are defined in <see cref="Constants.CartCommunicationType" />.
        /// </summary>
        /// <value>Returns the type of the cart communication.</value>
        string CartCommunicationType { get; set; }

        /// <summary>
        /// Gets or sets the ERP connection timeout in seconds.
        /// </summary>
        /// <value>Returns the connection timeout.</value>
        int ConnectionTimeout { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if (live) cart communication is enabled for anonymous users.
        /// </summary>
        /// <value>Returns <c>true</c> if cart communication is enabled for anonymous users; otherwise, <c>false</c>.</value>
        bool EnableCartCommunicationForAnonymousUsers { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if live prices are retrieved from the ERP.
        /// </summary>
        /// <value>Returns <c>true</c> if live prices should be retrieved from the ERP; otherwise, <c>false</c>.</value>
        bool EnableLivePrices { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if product info should be lazy loaded.
        /// </summary>
        /// <value>Returns <c>true</c> if lazy loading for products is enabled; otherwise, <c>false</c>.</value>
        bool LazyLoadProductInfo { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if live product information should be retrieved for anonymous users.
        /// </summary>
        /// <value>Returns <c>true</c> if live product information is enabled for anonymous users; otherwise, <c>false</c>.</value>
        bool LiveProductInfoForAnonymousUsers { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if calls to the ERP for live product info should be retried in case of a failure.
        /// </summary>
        bool MakeRetryForLiveProductInformation { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if the product request should contain all product variants or only the product being requested.
        /// </summary>
        bool GetProductInformationForAllVariants { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if connection errors should be logged.
        /// </summary>
        bool LogConnectionErrors { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if debug info should be logged.
        /// </summary>
        bool LogDebugInfo { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if general info should be logged.
        /// </summary>
        bool LogGeneralErrors { get; set; }

        /// <summary>
        /// Gets or sets a value that determines the maximum file size for the log before roll-over or clipping is applied.
        /// </summary>
        /// <value>The maximum size of the log.</value>
        int LogMaxSize { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if response errors should be logged.
        /// </summary>
        bool LogResponseErrors { get; set; }

        /// <summary>
        /// Gets or sets the recipient's email address for notification emails.
        /// </summary>
        /// <value>The notification email.</value>
        string NotificationEmail { get; set; }

        /// <summary>
        /// Gets or sets the sender's email address for notification emails.
        /// </summary>
        /// <value>The notification email sender email.</value>
        string NotificationEmailSenderEmail { get; set; }

        /// <summary>
        /// Gets or sets the sender's name for notification emails.
        /// </summary>
        /// <value>The name of the notification email sender.</value>
        string NotificationEmailSenderName { get; set; }

        /// <summary>
        /// Gets or sets the subject of the notification email.
        /// </summary>
        /// <value>The notification email subject.</value>
        string NotificationEmailSubject { get; set; }

        /// <summary>
        /// Gets or sets the frequency at which notifications should be sent.
        /// </summary>
        /// <value>The notification sending frequency.</value>
        string NotificationSendingFrequency { get; set; }

        /// <summary>
        /// Gets or sets the template used for the notification email.
        /// </summary>
        /// <value>The notification template.</value>
        string NotificationTemplate { get; set; }

        /// <summary>
        /// Gets or sets the cache level for order information.
        /// </summary>
        /// <value>The order cache level.</value>
        string OrderCacheLevel { get; set; }

        /// <summary>
        /// Gets or sets the order state that is applied after order export failed.
        /// </summary>
        /// <value>The order state after export failed.</value>
        string OrderStateAfterExportFailed { get; set; }

        /// <summary>
        /// Gets or sets the order state that is applied after order export succeeded.
        /// </summary>
        /// <value>The order state after export succeeded.</value>
        string OrderStateAfterExportSucceeded { get; set; }

        /// <summary>
        /// Gets or sets a value that determines whether to skip sending ledger entries to the ERP.
        /// </summary>
        bool SkipLedgerOrder { get; set; }

        /// <summary>
        /// Gets or sets the cache level for products.
        /// </summary>
        /// <value>The product cache level.</value>
        string ProductCacheLevel { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if orders can be queued to be exported later.
        /// </summary>
        /// <value>Returns <c>true</c> if orders can be queued for later export; otherwise, <c>false</c>.</value>
        bool QueueOrdersToExport { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if a copy of the original order xml should be saved.
        /// </summary>
        /// <value><c>true</c> if [save copy of order XML]; otherwise, <c>false</c>.</value>
        bool SaveCopyOfOrderXml { get; set; }

        /// <summary>
        /// Gets or sets the security key used to communicate with the ERP service.
        /// </summary>
        /// <value>The security key.</value>
        string SecurityKey { get; set; }

        /// <summary>
        /// Gets or sets the ID of the shop used in this integration.
        /// </summary>
        /// <value>The shop identifier.</value>
        string ShopId { get; set; }

        /// <summary>
        /// Gets or sets the text for product discounts.
        /// </summary>
        /// <value>The text for product discounts.</value>
        string ProductDiscountText { get; set; }

        /// <summary>
        /// Gets or sets the text for order discount order line.
        /// </summary>
        /// <value>The text for order discount order line.</value>
        string OrderDiscountText { get; set; }

        /// <summary>
        /// Gets or sets if ERP controls discount calculations
        /// </summary>
        /// <value><c>true</c> if [ERP controls discount calculations]; otherwise, <c>false</c>.</value>
        bool ErpControlsDiscount { get; set; }
        
        /// <summary>
        /// Gets or sets the key for shipping item type.
        /// </summary>
        /// <value>The key for shipping item type.</value>
        string ErpShippingItemType { get; set; }

        /// <summary>
        /// Gets or sets the key for shipping item.
        /// </summary>
        /// <value>The key for shipping item.</value>
        string ErpShippingItemKey { get; set; }

        /// <summary>
        /// Gets or sets if ERP controls shipping calculations
        /// </summary>
        /// <value><c>true</c> if [ERP controls shipping calculations]; otherwise, <c>false</c>.</value>
        bool ErpControlsShipping { get; set; }

        /// <summary>
        /// Gets or sets if the orderline should be set to fixed when unitprice is set by the orderhandler
        /// </summary>
        bool SetOrderlineFixed { get; set; }

        /// <summary>
        /// Gets or sets the url for the Web Service.
        /// </summary>
        /// <value>The web service URI.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "URI", Justification = "Could not be renamed now as it requires renaming in BaseLiveIntegrationAddIn in the Dynamicweb.Ecommerce package")]
        string WebServiceURI { get; set; }

        /// <summary>
        /// Gets or sets the ids for the Endpoint.
        /// </summary>
        /// <value>The Endpoint ids.</value>
        string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets a value that determines whether to use the product number in price calculations.
        /// </summary>
        /// <value><c>true</c> if [calculate price using product number]; otherwise, <c>false</c>.</value>
        bool CalculatePriceUsingProductNumber { get; set; }

        /// <summary>
        /// Gets or sets the number format culture.
        /// </summary>
        /// <value>The number format culture.</value>
        string NumberFormatCulture { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether to keep log files or to truncate a single log file.
        /// </summary>
        bool KeepLogFiles { get; set; }

        /// <summary>
        /// Global tag for connection availability
        /// </summary>
        /// <value>The name of the web service connection status global tag.</value>
        string WebServiceConnectionStatusGlobalTagName { get; set; }

        /// <summary>
        /// Gets or sets the value for the ping interval.
        /// </summary>
        /// <value>The interval between pings (seconds).</value>
        int AutoPingInterval { get; set; }

        /// <summary>
        /// Gets or sets if use unit prices
        /// </summary>
        /// <value><c>true</c> if [use unit prices]; otherwise, <c>false</c>.</value>
        bool UseUnitPrices { get; set; }

        /// <summary>
        /// Gets or sets a value that determines whether to use the product number in order calculations.
        /// </summary>
        /// <value><c>true</c> if [calculate order using product number]; otherwise, <c>false</c>.</value>
        bool CalculateOrderUsingProductNumber { get; set; }

        /// <summary>
        /// Gets or sets a value that determines the max number of products on each live request.
        /// </summary>
        int MaxProductsPerRequest { get; set; }
    }
}