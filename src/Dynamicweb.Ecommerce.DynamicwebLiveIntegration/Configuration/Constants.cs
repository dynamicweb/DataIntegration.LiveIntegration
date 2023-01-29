namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration
{
    /// <summary>
    /// Global constants class for all constant values in this project.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Defines the name of the Live integration.
        /// </summary>
        public const string AddInName = "DynamicwebLiveIntegrationAddIn";

        public static readonly string LiveIntegrationSettingsKey = "/Globalsettings/Modules/LiveIntegration";

        /// <summary>
        /// Min timer ping interval
        /// </summary>
        internal static readonly int MinPingInterval = 30;
        internal static readonly int DefaultPingInterval = 60;

        /// <summary>
        /// Nested class with cache configuration constants.
        /// </summary>
        internal static class CacheConfiguration
        {        
            /// <summary>
            ///  Defines the cache key used for storing order info. 
            /// </summary>
            public static readonly string OrderCommunicationHash = "OrderCommunicationHash";
        }

        /// <summary>
        /// Defines the ways that the cart can communicate with the ERP.
        /// </summary>
        internal static class CartCommunicationType
        {
            /// <summary>
            /// Live integration does not send the cart or orders to the ERP.
            /// </summary>
            public const string None = "None";

            /// <summary>
            /// Info is sent to the ERP for cart calculation and completed orders.
            /// </summary>
            public const string Full = "Full";

            /// <summary>
            /// Info is sent to the ERP for completed orders only (which means Dynamicweb calculates the cart).
            /// </summary>
            public const string OnlyOnOrderComplete = "Only on Order Complete";
        }

        /// <summary>
        /// Nested class with order configuration constants.
        /// </summary>
        internal static class OrderConfiguration
        {
            /// <summary>
            /// Defines the text label for order discount order line textbox.
            /// </summary>
            public const string OrderDiscountText = "Text for order discount order line";

            /// <summary>
            /// Defines the text label for product discount order lines textbox.
            /// </summary>
            public const string ProductDiscountText = "Text for product discount order lines";

            public const string DefaultShippingItemType = "ItemCharge";
        }
    }
}