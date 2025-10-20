using Dynamicweb.Content;
using Dynamicweb.Core;
using Dynamicweb.DataIntegration.EndpointManagement;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.Integration;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Environment;
using Dynamicweb.Extensibility.AddIns;
using Dynamicweb.Extensibility.Editors;
using Dynamicweb.Rendering;
using Dynamicweb.Security.UserManagement;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration
{
    /// <summary>
    /// Live Integration add-in to handle all settings and UI for interaction with the ERP.
    /// </summary>
    /// <seealso cref="BaseLiveIntegrationAddIn" />
    /// <seealso cref="IDropDownOptions" />
    /// <seealso cref="ISettings" />
    [AddInName(Constants.AddInName)]
    [AddInLabel("ERP Live Integration")]
    [AddInIgnore(false)]
    [AddInUseParameterGrouping(true)]
    [AddInUseParameterOrdering(true)]
    public class LiveIntegrationAddIn : BaseLiveIntegrationAddIn, IParameterOptions, ISettings, IParameterVisibility
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LiveIntegrationAddIn"/> class.
        /// </summary>
        public LiveIntegrationAddIn()
        {
        }

        #region Configuration parameters

        #region General parameters

        /// <summary>
        /// Instance Id
        /// </summary>
        [AddInParameter("InstanceId")]
        [AddInParameterEditor(typeof(HiddenParameterEditor), "")]
        [AddInParameterGroup("General")]
        public string InstanceId { get; set; }

        /// <summary>
        /// Settings file
        /// </summary>
        [AddInParameter("SettingsFile")]
        [AddInParameterEditor(typeof(HiddenParameterEditor), "")]
        [AddInParameterGroup("General")]
        public string SettingsFile { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if live integration is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        [AddInParameter("Enable live integration")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("General")]
        [AddInParameterOrder(5)]
        public bool IsLiveIntegrationEnabled { get; set; } = true;

        /// <summary>
        /// Instance Label
        /// </summary>
        [AddInParameter("Label")]
        [AddInParameterEditor(typeof(TextParameterEditor), "")]
        [AddInParameterGroup("General")]
        [AddInParameterOrder(6)]
        public string InstanceLabel { get; set; }

        [AddInParameterGroup("General")]
        [AddInParameter("ConnectionToType")]
        [AddInLabel("Connect to")]
        [AddInParameterEditor(typeof(RadioParameterEditor), "")]
        [AddInParameterOrder(9)]
        public string ConnectionToType { get; set; } = nameof(ConnectionType.Endpoint);

        /// <summary>
        /// The web service Uri
        /// </summary>
        /// <value>The web service URI.</value>
        [AddInParameter("Web service URL")]
        [AddInParameterEditor(typeof(TextParameterEditor), "TextArea=True;")]
        [AddInParameterGroup("General")]
        [AddInParameterOrder(10)]
        public override string WebServiceURI { get; set; }

        /// <summary>
        /// The security key.
        /// </summary>
        /// <value>The security key.</value>
        [AddInParameter("Security key")]
        [AddInParameterEditor(typeof(TextParameterEditor), "password=true;")]
        [AddInParameterGroup("General")]
        [AddInParameterOrder(20)]
        public override string SecurityKey { get; set; }

        /// <summary>
        /// Gets or sets the id for the Endpoint.
        /// </summary>
        /// <value>The Endpoint id.</value>        
        [AddInParameter("Endpoint")]
        [AddInParameterEditor(typeof(SelectionBoxParameterEditor), "multiple=true;none=true;InfoBar=true;explanation=Select one or more endpoints exposing a plug-in unit from Dynamicweb")]
        [AddInParameterGroup("General")]
        [AddInParameterOrder(20)]
        public string Endpoint { get; set; }

        /// <summary>
        /// Gets or sets the ERP connection timeout in seconds.
        /// </summary>
        /// <value>The connection timeout.</value>
        [AddInParameter("Connection timeout (seconds)")]
        [AddInParameterEditor(typeof(IntegerNumberParameterEditor), "minValue=30;Info=Minimum value is 30")]
        [AddInParameterGroup("General")]
        [AddInParameterOrder(30)]
        public int ConnectionTimeout { get; set; } = Constants.DefaultConnectionTimeout;

        /// <summary>
        /// Interval between pings (seconds)
        /// </summary>
        /// <value>The ping interval.</value>
        [AddInParameter("Interval between pings (seconds)")]
        [AddInDescription("When a error occurs in communication an auto ping will start to check when the ERP is back online.")]
        [AddInParameterEditor(typeof(IntegerNumberParameterEditor), "minValue=30;Info=Minimum value is 30")]
        [AddInParameterGroup("General")]
        [AddInParameterOrder(32)]
        public int AutoPingInterval { get; set; } = Constants.DefaultPingInterval;

        /// <summary>
        /// Gets or sets ShopId
        /// </summary>
        /// <value>The shop identifier.</value>
        [AddInParameter("Shop")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "none=false")]
        [AddInParameterGroup("General")]
        [AddInParameterOrder(40)]
        public string ShopId { get; set; }

        /// <summary>
        /// Gets or sets Website
        /// </summary>
        /// <value>The website identifier.</value>
        [AddInParameter("Website")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "none=true;nonetext=Any")]
        [AddInParameterGroup("General")]
        [AddInParameterOrder(43)]
        public int AreaId { get; set; }

        /// <summary>
        /// Number format culture
        /// </summary>
        /// <value>The number format culture.</value>
        [AddInParameter("Number format culture")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "none=true")]
        [AddInParameterGroup("General")]
        [AddInParameterOrder(50)]
        public string NumberFormatCulture { get; set; }

        /// <summary>
        /// Global tag for connection availability
        /// </summary>
        /// <value>The name of the web service connection status global tag.</value>
        [AddInParameter("Global tag for connection availability")]
        [AddInParameterEditor(typeof(TextParameterEditor), "")]
        [AddInParameterGroup("General")]
        [AddInParameterOrder(60)]
        public string WebServiceConnectionStatusGlobalTagName { get; set; }

        #endregion General parameters

        #region Products parameters

        /// <summary>
        /// Gets or sets a value that determines if live prices are retrieved from the ERP.
        /// </summary>
        /// <value><c>true</c> if [enable live prices]; otherwise, <c>false</c>.</value>
        [AddInParameter("Enable live prices")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Products")]
        [AddInParameterOrder(63)]
        public bool EnableLivePrices { get; set; } = true;

        /// <summary>
        /// Gets or sets lazy load product info
        /// </summary>
        /// <value><c>true</c> if [lazy load product information]; otherwise, <c>false</c>.</value>
        [AddInParameter("Lazy load product info (&getproductinfo=true)")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Products")]
        [AddInParameterOrder(65)]
        public bool LazyLoadProductInfo { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if custom product fields are appended to the outgoing XML request.
        /// </summary>
        /// <value><c>true</c> if [add product fields to request]; otherwise, <c>false</c>.</value>
        [AddInParameter("Include product custom fields in request")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Products")]
        [AddInParameterOrder(70)]
        public bool AddProductFieldsToRequest { get; set; } = true;

        /// <summary>
        /// Gets or sets product cache level
        /// </summary>
        /// <value>The product cache level.</value>
        [AddInParameter("Product information cache level")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "none=false")]
        [AddInParameterGroup("Products")]
        [AddInParameterOrder(80)]
        public string ProductCacheLevel { get; set; }

        /// <summary>
        /// Use product number in price calculation
        /// </summary>
        /// <value><c>true</c> if [calculate price using product number]; otherwise, <c>false</c>.</value>
        [AddInParameter("Use product number in price calculation")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Products")]
        [AddInParameterOrder(90)]
        public bool CalculatePriceUsingProductNumber { get; set; } = false;

        /// <summary>
        /// Gets or sets if use unit prices
        /// </summary>
        /// <value><c>true</c> if [use unit prices]; otherwise, <c>false</c>.</value>
        [AddInParameter("Use unit prices")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Products")]
        [AddInParameterOrder(95)]
        public bool UseUnitPrices { get; set; } = false;

        /// <summary>
        /// Defines if the call to the ERP should make retry in the Live Products calls
        /// </summary>
        [AddInParameter("Retry request for the product information")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Products")]
        [AddInParameterOrder(100)]
        public bool MakeRetryForLiveProductInformation { get; set; } = false;

        /// <summary>
        /// Defines if the product request should contain all product variants or only the product being requested
        /// </summary>
        [AddInParameter("Include variants in the product information request")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Products")]
        [AddInParameterOrder(110)]
        public bool GetProductInformationForAllVariants { get; set; } = false;

        /// <summary>
        /// Gets or sets a value that determines the max number of products on each live request.
        /// </summary>
        [AddInParameter("Max products per request")]
        [AddInParameterEditor(typeof(IntegerNumberParameterEditor), "minValue=0")]
        [AddInParameterGroup("Products")]
        [AddInParameterOrder(120)]
        public int MaxProductsPerRequest { get; set; } = 0;

        #endregion Products parameters

        #region Orders parameters

        /// <summary>
        /// Gets or sets the type of communication used for the cart. Valid options are defined in <see cref="Constants.CartCommunicationType" />.
        /// </summary>
        /// <value>The type of the cart communication.</value>
        [AddInParameter("Cart communication type")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "none=false")]
        [AddInParameterGroup("Orders")]
        [AddInParameterOrder(111)]
        public string CartCommunicationType { get; set; }

        /// <summary>
        /// Gets or sets if queue orders to export
        /// </summary>
        /// <value><c>true</c> if [queue orders to export]; otherwise, <c>false</c>.</value>
        [AddInParameter("Queue orders (and allow payments) if no connection")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Orders")]
        [AddInParameterOrder(112)]
        public bool QueueOrdersToExport { get; set; } = true;

        /// <summary>
        /// Gets or sets a value that determines if custom order fields are appended to the outgoing XML request.
        /// </summary>
        /// <value><c>true</c> if [add order fields to request]; otherwise, <c>false</c>.</value>
        [AddInParameter("Include order custom fields in request")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Orders")]
        [AddInParameterOrder(115)]
        public bool AddOrderFieldsToRequest { get; set; } = true;

        /// <summary>
        /// Gets or sets a value that determines if custom order line fields are appended to the outgoing XML request.
        /// </summary>
        /// <value><c>true</c> if order line fields should be added to request; otherwise, <c>false</c>.</value>
        [AddInParameter("Include order line custom fields in request")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Orders")]
        [AddInParameterOrder(120)]
        public bool AddOrderLineFieldsToRequest { get; set; } = true;

        /// <summary>
        /// Gets or sets a value that determines if parts information (BOM) is appended to the outgoing XML request.
        /// </summary>
        /// <value><c>true</c> if order line parts should be added to request; otherwise, <c>false</c>.</value>
        [AddInParameter("Include parts order lines in request")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Orders")]
        [AddInParameterOrder(130)]
        public bool AddOrderLinePartsToRequest { get; set; } = true;

        /// <summary>
        /// Gets or sets if save copy of order xml
        /// </summary>
        /// <value><c>true</c> if [save copy of order XML]; otherwise, <c>false</c>.</value>
        [AddInParameter("Save copy of order XML request")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Orders")]
        [AddInParameterOrder(140)]
        public bool SaveCopyOfOrderXml { get; set; }

        /// <summary>
        /// Use product number in order calculation
        /// </summary>
        /// <value><c>true</c> if [calculate order using product number]; otherwise, <c>false</c>.</value>
        [AddInParameter("Use product number in order calculation")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Orders")]
        [AddInParameterOrder(145)]
        public bool CalculateOrderUsingProductNumber { get; set; } = false;

        /// <summary>
        /// Gets or sets text for product discount orderlines
        /// </summary>
        /// <value>The text for product discount orderlines.</value>
        [AddInParameter(Constants.OrderConfiguration.ProductDiscountText)]
        [AddInParameterEditor(typeof(TextParameterEditor), "")]
        [AddInParameterGroup("Orders")]
        [AddInParameterOrder(150)]
        public string ProductDiscountText { get; set; }

        /// <summary>
        /// Gets or sets text for order discount order line
        /// </summary>
        /// <value>The text for order discount order line.</value>
        [AddInParameter(Constants.OrderConfiguration.OrderDiscountText)]
        [AddInParameterEditor(typeof(TextParameterEditor), "")]
        [AddInParameterGroup("Orders")]
        [AddInParameterOrder(153)]
        public string OrderDiscountText { get; set; }

        /// <summary>
        /// Gets or sets if ERP controls discount calculations
        /// </summary>
        /// <value><c>true</c> if [ERP controls discount calculations]; otherwise, <c>false</c>.</value>
        [AddInParameter("ERP controls discount calculations")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Orders")]
        [AddInParameterOrder(156)]
        public bool ErpControlsDiscount { get; set; }

        /// <summary>
        /// Gets or sets if ERP controls shipping calculations
        /// </summary>
        /// <value><c>true</c> if [ERP controls shipping calculations]; otherwise, <c>false</c>.</value>
        [AddInParameter("ERP controls shipping calculations")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Orders")]
        [AddInParameterOrder(157)]
        public bool ErpControlsShipping { get; set; }

        /// <summary>
        /// Gets or sets the key for shipping item type.
        /// </summary>
        /// <value>The key for shipping item.</value>
        [AddInParameter("ERP shipping item type")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "none=false")]
        [AddInParameterGroup("Orders")]
        [AddInParameterOrder(158)]
        public string ErpShippingItemType { get; set; }

        /// <summary>
        /// Gets or sets the key for shipping item.
        /// </summary>
        /// <value>The key for shipping item.</value>
        [AddInParameter("ERP shipping item key")]
        [AddInParameterEditor(typeof(TextParameterEditor), "")]
        [AddInParameterGroup("Orders")]
        [AddInParameterOrder(159)]
        public string ErpShippingItemKey { get; set; }

        /// <summary>
        /// Gets or sets if the orderline should be set to fixed when unitprice is set by the orderhandler
        /// </summary>
        [AddInParameter("Set orderline type to fixed")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Orders")]
        [AddInParameterOrder(157)]
        public bool SetOrderlineFixed { get; set; }

        /// <summary>
        /// Gets or sets order state after export succeeded
        /// </summary>
        /// <value>The order state after export succeeded.</value>
        [AddInParameter("Order state after export succeeded")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "none=false;SortBy=Key")]
        [AddInParameterGroup("Orders")]
        [AddInParameterOrder(160)]
        public string OrderStateAfterExportSucceeded { get; set; }

        /// <summary>
        /// Gets or sets order state after export failed
        /// </summary>
        /// <value>The order state after export failed.</value>
        [AddInParameter("Order state after export failed")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "none=false;SortBy=Key")]
        [AddInParameterGroup("Orders")]
        [AddInParameterOrder(170)]
        public string OrderStateAfterExportFailed { get; set; }

        /// <summary>
        /// Gets or sets order cache level
        /// </summary>
        /// <value>The order cache level.</value>
        [AddInParameter("Order cache level")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "none=false")]
        [AddInParameterGroup("Orders")]
        [AddInParameterOrder(180)]
        public string OrderCacheLevel { get; set; }


        /// <summary>
        /// Gets or sets if skip ledgers
        /// </summary>
        [AddInParameter("Do not process ledger order")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Orders")]
        [AddInParameterOrder(190)]
        public bool SkipLedgerOrder { get; set; }

        #endregion Orders parameters

        #region Users parameters
        /// <summary>
        /// Gets or sets live product information for Anonymous users
        /// </summary>
        /// <value><c>true</c> if [live product information for anonymous users]; otherwise, <c>false</c>.</value>
        [AddInParameter("Live product info for anonymous users")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Users")]
        [AddInParameterOrder(200)]
        public bool LiveProductInfoForAnonymousUsers { get; set; }

        /// <summary>
        /// Gets or sets a value that determines if (live) cart communication is enabled for anonymous users.
        /// </summary>
        /// <value><c>true</c> if [enable cart communication for anonymous users]; otherwise, <c>false</c>.</value>
        [AddInParameter("Cart and orders for anonymous users")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Users")]
        [AddInParameterOrder(210)]
        public bool EnableCartCommunicationForAnonymousUsers { get; set; }

        /// <summary>
        /// Gets or sets the customer name used in integration scenarios with anonymous users.
        /// </summary>
        /// <value>The anonymous user key.</value>
        [AddInParameter("ERP Anonymous user key")]
        [AddInParameterEditor(typeof(TextParameterEditor), "")]
        [AddInParameterGroup("Users")]
        [AddInParameterOrder(230)]
        public string AnonymousUserKey { get; set; }

        #endregion Users parameters

        #region Notifications parameters

        /// <summary>
        /// Gets or sets notification email
        /// </summary>
        /// <value>The notification email.</value>
        [AddInParameter("Notification recipient e-mail")]
        [AddInParameterEditor(typeof(TextParameterEditor), "")]
        [AddInParameterGroup("Notifications")]
        [AddInParameterOrder(230)]
        public string NotificationEmail { get; set; }

        /// <summary>
        /// Gets or sets notification email
        /// </summary>
        /// <value>The notification email.</value>
        [AddInParameter("Notification recipient groups")]
        [AddInParameterEditor(typeof(UserGroupParameterEditor), "Multiple=true;")]
        [AddInParameterGroup("Notifications")]
        [AddInParameterOrder(235)]
        public string RecipientGroups { get; set; }

        /// <summary>
        /// Gets or sets notification template
        /// </summary>
        /// <value>The notification template.</value>
        [AddInParameter("Notification e-mail template")]
        [AddInParameterEditor(typeof(TemplateParameterEditor), "folder=Templates/DataIntegration/Notifications")]
        [AddInParameterGroup("Notifications")]
        [AddInParameterOrder(240)]
        public string NotificationTemplate { get; set; }

        /// <summary>
        /// Gets or sets notification subject
        /// </summary>
        /// <value>The notification email subject.</value>
        [AddInParameter("Notification e-mail subject")]
        [AddInParameterEditor(typeof(TextParameterEditor), "")]
        [AddInParameterGroup("Notifications")]
        [AddInParameterOrder(250)]
        public string NotificationEmailSubject { get; set; }

        /// <summary>
        /// Gets or sets notification sender name
        /// </summary>
        /// <value>The name of the notification email sender.</value>
        [AddInParameter("Notification e-mail sender name")]
        [AddInParameterEditor(typeof(TextParameterEditor), "")]
        [AddInParameterGroup("Notifications")]
        [AddInParameterOrder(260)]
        public string NotificationEmailSenderName { get; set; }

        /// <summary>
        /// Gets or sets notification sender email
        /// </summary>
        /// <value>The notification email sender email.</value>
        [AddInParameter("Notification sender e-mail")]
        [AddInParameterEditor(typeof(TextParameterEditor), "")]
        [AddInParameterGroup("Notifications")]
        [AddInParameterOrder(270)]
        public string NotificationEmailSenderEmail { get; set; }

        /// <summary>
        /// Gets or sets notification frequency
        /// </summary>
        /// <value>The notification sending frequency.</value>
        [AddInParameter("Notification sending frequency")]
        [AddInParameterEditor(typeof(DropDownParameterEditor), "none=false;SortBy=Key")]
        [AddInParameterGroup("Notifications")]
        [AddInParameterOrder(280)]
        public string NotificationSendingFrequency { get; set; }
        #endregion Notifications parameters

        #region Logs parameters

        /// <summary>
        /// Gets or sets log max file size
        /// </summary>
        /// <value>The maximum size of the log.</value>
        [AddInParameter("Log file max size (MB)")]
        [AddInParameterEditor(typeof(TextParameterEditor), "")]
        [AddInParameterGroup("Logs")]
        [AddInParameterOrder(320)]
        public int LogMaxSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to keep log files or to truncate a single log file
        /// </summary>
        /// <value><c>true</c> if [keep log files]; otherwise, <c>false</c>.</value>
        [AddInParameter("Keep all log files")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Logs")]
        [AddInParameterOrder(330)]
        public bool KeepLogFiles { get; set; }

        /// <summary>
        /// Gets or sets if to log errors
        /// </summary>
        /// <value><c>true</c> if [log general errors]; otherwise, <c>false</c>.</value>
        [AddInParameter("Log general errors")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Logs")]
        [AddInParameterOrder(340)]
        public bool LogGeneralErrors { get; set; }

        /// <summary>
        /// Gets or sets if to log connection errors
        /// </summary>
        /// <value><c>true</c> if [log connection errors]; otherwise, <c>false</c>.</value>
        [AddInParameter("Log connection errors")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Logs")]
        [AddInParameterOrder(350)]
        public bool LogConnectionErrors { get; set; }

        /// <summary>
        /// Gets or sets if to log response errors
        /// </summary>
        /// <value><c>true</c> if [log response errors]; otherwise, <c>false</c>.</value>
        [AddInParameter("Log response errors")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Logs")]
        [AddInParameterOrder(360)]
        public bool LogResponseErrors { get; set; }

        /// <summary>
        /// Gets or sets to log debug information
        /// </summary>
        /// <value><c>true</c> if [log debug information]; otherwise, <c>false</c>.</value>
        [AddInParameter("Log request and response content")]
        [AddInParameterEditor(typeof(YesNoParameterEditor), "")]
        [AddInParameterGroup("Logs")]
        [AddInParameterOrder(370)]
        public bool LogDebugInfo { get; set; }

        #endregion Logs parameters        

        #endregion Configuration parameters

        /// <summary>
        /// Get options for editors with multiple values
        /// </summary>
        /// <param name="dropdownName">Name of the dropdown.</param>
        /// <returns>Hashtable.</returns>
        IEnumerable<ParameterOption> IParameterOptions.GetParameterOptions(string dropdownName)
        {
            var options = new List<ParameterOption>();

            switch (dropdownName)
            {
                case "Notification sending frequency":
                    foreach (NotificationFrequency frequencyLevel in Enum.GetValues(typeof(NotificationFrequency)))
                    {
                        options.Add(new(GetNotificationFrequencyText(frequencyLevel), ((int)frequencyLevel).ToString()));
                    }
                    break;

                case "Order state after export succeeded":
                case "Order state after export failed":
                    options.Add(new("Leave unchanged", string.Empty));
                    foreach (var state in Services.OrderStates.GetStatesByOrderType(OrderType.Order))
                    {
                        if (state.IsDeleted)
                            continue;

                        options.Add(new(state.GetName(Services.Languages.GetDefaultLanguageId()), state.Id));
                    }
                    break;

                case "Shop":
                    options.Add(new("Any", string.Empty));
                    var shops = Services.Shops.GetShops();
                    foreach (var shop in shops)
                    {
                        options.Add(new(shop.Name, shop.Id));
                    }
                    break;
                case "Website":
                    foreach (var area in new AreaService().GetAreas())
                    {
                        options.Add(new(area.Name, area.ID));
                    }
                    break;
                case "Cart communication type":
                    options.Add(new(Constants.CartCommunicationType.None, Constants.CartCommunicationType.None));
                    options.Add(new(Constants.CartCommunicationType.Full, Constants.CartCommunicationType.Full));
                    options.Add(new(Constants.CartCommunicationType.OnlyOnOrderComplete, Constants.CartCommunicationType.OnlyOnOrderComplete));
                    options.Add(new(Constants.CartCommunicationType.CartOnly, Constants.CartCommunicationType.CartOnly));
                    break;

                case "Number format culture":
                    foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
                    {
                        if (!string.IsNullOrEmpty(culture.Name) && !culture.IsNeutralCulture && !options.Any(o => string.Equals(o.Value.ToString(), culture.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                            options.Add(new($"{culture.Name} - {culture.EnglishName}", culture.Name));
                        }
                    }
                    break;
                case "Order cache level":
                case "Product information cache level":
                    foreach (var cacheLevel in Enum.GetNames(typeof(Cache.ResponseCacheLevel)))
                    {
                        options.Add(new(cacheLevel, cacheLevel));
                    }
                    break;
                case "Endpoint":
                    EndpointService endpointService = new EndpointService();
                    Dictionary<int, string> endpointFilters = new Dictionary<int, string>();
                    if (!string.IsNullOrEmpty(Endpoint))
                    {
                        foreach (string str in Endpoint.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (str.Contains(";"))
                            {
                                int id = Core.Converter.ToInt32(UrlHandler.GetUrl(str));
                                if (id > 0)
                                {
                                    if (!endpointFilters.ContainsKey(id))
                                    {
                                        endpointFilters.Add(id, str);
                                    }
                                }
                            }
                        }
                    }
                    foreach (Endpoint endpoint in endpointService.GetEndpoints().Where(e => e != null && endpointService.IsDynamicwebServiceEndpoint(e) && e.UseInLiveIntegration))
                    {
                        if (endpointFilters.ContainsKey(endpoint.Id))
                        {
                            string str = endpointFilters[endpoint.Id];
                            options.Add(new(endpoint.Name + str.Substring(str.IndexOf(";")), str));
                        }
                        else
                        {
                            options.Add(new(endpoint.Name, endpoint.Id.ToString()));
                        }
                    }
                    break;
                case "ERP shipping item type":
                    options.Add(new("Item Charge", Constants.OrderConfiguration.DefaultShippingItemType));
                    options.Add(new("G/L Account", "Account"));
                    options.Add(new("Item", "Item"));
                    options.Add(new("Fixed Asset", "FixedAsset"));
                    options.Add(new("Resource", "Resource"));
                    break;
                case "ConnectionToType":
                    options.Add(new(nameof(ConnectionType.Endpoint), ConnectionType.Endpoint));
                    options.Add(new("Dynamicweb connector web service", ConnectionType.WebService));
                    break;
                default:
                    throw new ArgumentException($"Unsupported dropdown: {dropdownName}", nameof(dropdownName));
            }

            return options;
        }

        IEnumerable<string> IParameterVisibility.GetHiddenParameterNames(string parameterName, object? parameterValue)
        {
            var result = new List<string>();
            var parameterValueStr = Converter.ToString(parameterValue);
            switch (parameterName)
            {
                case nameof(ConnectionToType):
                    if (nameof(ConnectionType.Endpoint).Equals(parameterValueStr, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add("Web service URL");
                        result.Add("Security key");
                    }
                    else
                    {
                        result.Add("Endpoint");
                    }
                    break;
            }
            return result;
        }

        private Settings GetCurrentSettings()
        {
            Settings settings = null;
            string id = SettingsManager.GetCurrentInstanceId();
            if (string.IsNullOrEmpty(id))
            {
                id = InstanceId;
            }
            if (!string.IsNullOrEmpty(id))
            {
                settings = SettingsManager.GetSettingsById(id);
            }
            if (settings == null)
            {
                settings = new Settings();
            }
            return settings;
        }

        /// <summary>
        /// Check if the web service is available for the live integration
        /// </summary>
        /// <returns><c>true</c> if [is web service connection available]; otherwise, <c>false</c>.</returns>
        public override bool IsWebServiceConnectionAvailable()
        {
            Settings settings = GetCurrentSettings();

            var logger = new Logger(settings);
            string multipleUrlsText = settings?.Endpoint;
            if (!string.IsNullOrEmpty(multipleUrlsText))
            {
                EndpointConnector endpointConnector = new EndpointConnector(settings, logger, SubmitType.Backend);
                return endpointConnector.IsConnectionAvailableFromBackend(multipleUrlsText);
            }
            else
            {
                WebServiceConnector webServiceConnector = new WebServiceConnector(settings, logger, SubmitType.Backend);
                return webServiceConnector.IsConnectionAvailableFromBackend(settings.WebServiceURI);
            }
        }

        /// <summary>
        /// Load add-in configuration settings
        /// </summary>
        /// <returns>Add-in settings in the Xml format</returns>
        /// <remarks>The Xml format must be suitable for use in Dynamicweb.Extensibility.AddInSelector</remarks>
        public override string LoadSettings()
        {
            Settings settings = GetCurrentSettings();
            Settings.UpdateFrom(settings, this);

            if (string.IsNullOrEmpty(Endpoint) && !string.IsNullOrEmpty(WebServiceURI))
            {
                ConnectionToType = nameof(ConnectionType.WebService);
            }

            var xml = GetParametersToXml(false);

            return xml;
        }

        /// <summary>
        /// Save Add-in configuration settings
        /// </summary>
        public override void SaveSettings()
        {
            Settings settings = GetCurrentSettings();
            // Do not allow Endpoint and WebService to be selected at the same time
            if (!string.IsNullOrEmpty(Endpoint) && !string.IsNullOrEmpty(WebServiceURI)
                && Enum.TryParse<ConnectionType>(ConnectionToType, out var connectionType) && connectionType == ConnectionType.WebService)
            {
                Endpoint = "";
            }

            Settings.UpdateFrom(this, settings);
            if (string.IsNullOrEmpty(settings.InstanceId))
            {
                string id = SettingsManager.GetCurrentInstanceId();
                if (string.IsNullOrEmpty(id))
                {
                    id = SettingsManager.GetNextNewInstanceId();
                }
                settings.InstanceId = id;
                InstanceId = id;
            }
            if (AutoPingInterval < Constants.MinPingInterval)
            {
                AutoPingInterval = Constants.MinPingInterval;
            }
            if (ConnectionTimeout < Constants.DefaultConnectionTimeout)
            {
                ConnectionTimeout = Constants.DefaultConnectionTimeout;
            }
            SettingsFileHandler handler = new SettingsFileHandler();
            handler.SaveSettings(settings);
            Licensing.LicenseService.SaveSettings(settings);
            SaveTranslations(settings);
            Connector.ClearCache();
            Logger.ClearLogMessages(settings);
        }

        /// <summary>
        /// Return Integration customer center calls
        /// </summary>        
        public override List<string> GetSupportedCustomerCenterIntegrationCalls()
        {
            return new List<string>() { "OpenOrder", "Invoice", "Credit", "SalesShipment" };
        }

        /// <summary>
        /// Retrieves integration customer center item details
        /// </summary>
        /// <param name="template">Template to render item details</param>
        /// <param name="callType">Item type(OpenOrder,Invoice,Credit,SalesShipment)</param>
        /// <param name="user">User</param>
        /// <param name="itemID">item id</param>
        /// <returns>Template with rendered item details</returns>
        public override Template RetrieveItemDetailsFromRemoteSystem(Template template, string callType, User user, string itemID)
        {
            return IntegrationCustomerCenterHandler.RetrieveItemDetailsFromRemoteSystem(template, callType, user, itemID);
        }

        /// <summary>
        /// Retrieves integration customer center items list
        /// </summary>
        /// <param name="template">Template to render items list</param>
        /// <param name="callType">Item type(OpenOrder,Invoice,Credit,SalesShipment)</param>
        /// <param name="user">User</param>
        /// <param name="pageSize">List page size</param>
        /// <param name="pageIndex">List page index</param>
        /// <param name="totalItemsCount">Items count</param>
        /// <returns>Template with rendered items list</returns>
        public override Template RetrieveItemsListFromRemoteSystem(Template template, string callType, User user, int pageSize, int pageIndex, ref int totalItemsCount)
        {
            return IntegrationCustomerCenterHandler.RetrieveItemsListFromRemoteSystem(template, callType, user, pageSize, pageIndex, out totalItemsCount);
        }

        /// <summary>
        /// Retrieves order information in pdf
        /// </summary>
        public override string RetrievePDF(IRequest request)
        {
            return IntegrationCustomerCenterHandler.RetrievePDF(request);
        }

        /// <summary>
        /// Gets the notification frequency text.
        /// </summary>
        /// <param name="frequency">The frequency.</param>
        /// <returns>System.String.</returns>
        private static string GetNotificationFrequencyText(NotificationFrequency frequency)
        {
            string result;
            switch (frequency)
            {
                case NotificationFrequency.Minute:
                    result = "1 minute";
                    break;

                case NotificationFrequency.FiveMinutes:
                    result = "5 minutes";
                    break;

                case NotificationFrequency.TenMinutes:
                    result = "10 minutes";
                    break;

                case NotificationFrequency.FifteenMinutes:
                    result = "15 minutes";
                    break;

                case NotificationFrequency.ThirtyMinutes:
                    result = "30 minutes";
                    break;

                case NotificationFrequency.OneHour:
                    result = "1 hour";
                    break;

                case NotificationFrequency.TwoHours:
                    result = "2 hours";
                    break;

                case NotificationFrequency.ThreeHours:
                    result = "3 hours";
                    break;

                case NotificationFrequency.SixHours:
                    result = "6 hours";
                    break;

                case NotificationFrequency.TwelveHours:
                    result = "12 hours";
                    break;

                case NotificationFrequency.OneDay:
                    result = "1 day";
                    break;

                default:
                    result = "Never";
                    break;
            }

            return result;
        }

        private static void SaveTranslations(Settings settings)
        {
            Helpers.SaveTranslation(Constants.OrderConfiguration.OrderDiscountText, settings.OrderDiscountText);
            Helpers.SaveTranslation(Constants.OrderConfiguration.ProductDiscountText, settings.ProductDiscountText);
        }

        private enum ConnectionType
        {
            Endpoint,
            WebService,
        }
    }
}