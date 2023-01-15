using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Extensibility;
using Dynamicweb.Extensibility.Notifications;
using System;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications
{
    /// <summary>
    /// Notifications and argument classes for notification subscribers fired during generation of order XML.
    /// </summary>
    public static class Order
    {
        /// <summary>
        /// Occurs before the XML for a cart or order is generated. This enables you to change or analyze the order before it's processed.
        /// </summary>
        /// <example>
        /// <code description="Notification observer example" source="..\..\Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples\Notifications\OrderBeforeGenerateXmlSubscriber.cs" lang="CS"></code>
        /// </example>
        public const string OnBeforeGenerateOrderXml = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnBeforeGenerateOrderXml";

        /// <summary>
        /// Occurs after the XML for a cart or order is generated. This enables you to change or analyze the XML before it's sent to the ERP.
        /// </summary>
        /// <example>
        /// <code description="Notification observer example" source="..\..\Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples\Notifications\OrderAfterGenerateXmlSubscriber.cs" lang="CS"></code>
        /// </example>
        public const string OnAfterGenerateOrderXml = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnAfterGenerateOrderXml";

        /// <summary>
        /// Occurs before a cart or order is sent to the ERP.
        /// </summary>
        /// <example>
        /// <code description="Notification observer example" source="..\..\Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples\Notifications\OrderBeforeSendToErpSubscriber.cs" lang="CS"></code>
        /// </example>
        public const string OnBeforeSendingOrderToErp = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnBeforeSendingOrderToErp";

        /// <summary>
        /// Occurs after a cart or order has been sent to the ERP.
        /// </summary>
        /// <example>
        /// <code description="Notification observer example" source="..\..\Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples\Notifications\OrderAfterSendToErpSubscriber.cs" lang="CS"></code>
        /// </example>
        public const string OnAfterSendingOrderToErp = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnAfterSendingOrderToErp";

        /// <summary>
        /// Occurs before dynamicweb shipping is updated.
        /// </summary>
        /// <example>
        /// <code description="Notification observer example" source="..\..\Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples\Notifications\OrderOnBeforeUpdateDynamicwebShipping.cs" lang="CS"></code>
        /// </example>
        public const string OnBeforeUpdateDynamicwebShipping = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnBeforeUpdateDynamicwebShipping";

        /// <summary>
        /// Arguments class for the OnBeforeGenerateOrderXml subscriber.
        /// </summary>
        /// <seealso cref="NotificationArgs" />
        public class OnBeforeGenerateOrderXmlArgs : NotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnBeforeGenerateOrderXmlArgs"/> class.
            /// </summary>
            /// <param name="order">The order for which the XML is being generated.</param>
            /// <param name="settings">An instance of OrderXmlGeneratorSettings that determines how the XML is generated.</param>
            public OnBeforeGenerateOrderXmlArgs(Ecommerce.Orders.Order order, OrderXmlGeneratorSettings settings, Settings liveIntegrationSettings, Logger logger)
            {
                Order = order;
                GeneratorSettings = settings;
                Settings = liveIntegrationSettings;
                Logger = logger;
            }

            /// <summary>
            /// Gets the order for which the XML is being generated.
            /// </summary>
            /// <value>The order.</value>
            public Ecommerce.Orders.Order Order { get; }

            /// <summary>
            /// Gets the OrderXmlGeneratorSettings that determines how the XML is generated
            /// </summary>
            /// <value>The generator settings.</value>
            public OrderXmlGeneratorSettings GeneratorSettings { get; }

            /// <summary>
            /// Settings
            /// </summary>
            public Settings Settings { get; }

            /// <summary>
            /// Logger
            /// </summary>
            public Logger Logger { get; }
        }

        /// <summary>
        /// Arguments class for the OnAfterGenerateOrderXml subscriber.
        /// </summary>
        /// <seealso cref="NotificationArgs" />
        public class OnAfterGenerateOrderXmlArgs : NotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnAfterGenerateOrderXmlArgs"/> class.
            /// </summary>
            /// <param name="order">The order for which the XML has been generated.</param>
            /// <param name="settings">The settings used for generating the XML.</param>
            /// <param name="xmlDocument">The XML document that has been created. You can manipulate this document to alter the XML being sent to the ERP.</param>
            public OnAfterGenerateOrderXmlArgs(Ecommerce.Orders.Order order, OrderXmlGeneratorSettings settings, XmlDocument xmlDocument, Settings liveIntegrationSettings, Logger logger)
            {
                Order = order;
                GeneratorSettings = settings;
                Document = xmlDocument;
                Settings = liveIntegrationSettings;
                Logger = logger;
            }

            /// <summary>
            /// Gets the order for which the XML has been generated..
            /// </summary>
            /// <value>The order.</value>
            public Ecommerce.Orders.Order Order { get; }

            /// <summary>
            /// Gets the generator settings.
            /// </summary>
            /// <value>The generator settings.</value>
            public OrderXmlGeneratorSettings GeneratorSettings { get; }

            /// <summary>
            /// Gets the XML document that has been created. You can manipulate this document to alter the XML being sent to the ERP..
            /// </summary>
            /// <value>The document.</value>
            public XmlDocument Document { get; }

            /// <summary>
            /// Settings
            /// </summary>
            public Settings Settings { get; }

            /// <summary>
            /// Logger
            /// </summary>
            public Logger Logger { get; }
        }

        /// <summary>
        /// Arguments class for the OnBeforeSendingOrderToErp subscriber.
        /// When Cancel is true the order is not sent.
        /// </summary>
        /// <seealso cref="CancelableNotificationArgs" />
        public class OnBeforeSendingOrderToErpArgs : CancelableNotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnBeforeSendingOrderToErpArgs"/> class.
            /// </summary>
            /// <param name="order">The order that is about to be sent.</param>
            /// <param name="createOrder">True when the order should be created in the ERP, false when it's a cart calculation.</param>
            public OnBeforeSendingOrderToErpArgs(Ecommerce.Orders.Order order, bool createOrder, Settings liveIntegrationSettings, Logger logger)
            {
                Order = order;
                CreateOrder = createOrder;
                Settings = liveIntegrationSettings;
                Logger = logger;
            }

            /// <summary>
            /// Gets the order that is about to be sent.
            /// </summary>
            /// <value>The order.</value>
            public Ecommerce.Orders.Order Order { get; }

            /// <summary>
            /// Gets a value that indicates whether the order should be created in the ERP or just calculated as a cart.
            /// </summary>
            /// <value>True when the order should be created in the ERP, false when it's a cart calculation.</value>
            public bool CreateOrder { get; }

            /// <summary>
            /// Settings
            /// </summary>
            public Settings Settings { get; }

            /// <summary>
            /// Logger
            /// </summary>
            public Logger Logger { get; }
        }

        /// <summary>
        /// Arguments class for the OnAfterSendingOrderToErp subscriber.
        /// </summary>
        /// <seealso cref="NotificationArgs" />
        public class OnAfterSendingOrderToErpArgs : NotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnAfterSendingOrderToErpArgs"/> class.
            /// </summary>
            /// <param name="order">The order that has been sent to the ERP.</param>
            /// <param name="createOrder">True when the order should be created in the ERP, false when it's a cart calculation.</param>
            /// <param name="responseDocument">The XML document with the ERP response.</param>
            /// <param name="error">The error that occurred, of any.</param>
            public OnAfterSendingOrderToErpArgs(Ecommerce.Orders.Order order, bool createOrder, XmlDocument responseDocument, Exception error, Settings liveIntegrationSettings, Logger logger)
            {
                Order = order;
                CreateOrder = createOrder;
                ResponseDocument = responseDocument;
                Error = error;
                Settings = liveIntegrationSettings;
                Logger = logger;
            }

            /// <summary>
            /// Gets the order that has been sent to the ERP.
            /// </summary>
            /// <value>The order.</value>
            public Ecommerce.Orders.Order Order { get; }

            /// <summary>
            /// Gets a value that indicates whether the order should be created in the ERP or just calculated as a cart.
            /// </summary>
            /// <value>True when the order should be created in the ERP, false when it's a cart calculation.</value>
            public bool CreateOrder { get; }

            /// <summary>
            /// Gets the XML response document.
            /// </summary>
            /// <value>The response document.</value>
            public XmlDocument ResponseDocument { get; }

            /// <summary>
            /// Gets the error that occurred, if any.
            /// </summary>
            /// <value>The error.</value>
            public Exception Error { get; }

            /// <summary>
            /// Settings
            /// </summary>
            public Settings Settings { get; }

            /// <summary>
            /// Logger
            /// </summary>
            public Logger Logger { get; }
        }

        /// <summary>
        /// Arguments class for the OnBeforeUpdateDynamicwebShipping subscriber.
        /// </summary>
        /// <seealso cref="NotificationArgs" />
        public class OnBeforeUpdateDynamicwebShippingArgs : NotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnBeforeUpdateDynamicwebShippingArgs"/> class.
            /// </summary>
            /// <param name="order">The order that has been sent to the ERP.</param>
            /// <param name="orderNode">OrderNode from ERP response</param>
            /// <param name="shippingFeeSentInRequest">Shipping fee sent in the request.</param>            
            public OnBeforeUpdateDynamicwebShippingArgs(Ecommerce.Orders.Order order, XmlNode orderNode, PriceInfo shippingFeeSentInRequest, Settings liveIntegrationSettings, Logger logger)
            {
                Order = order;
                OrderNode = orderNode;
                ShippingFeeSentInRequest = shippingFeeSentInRequest;
                Settings = liveIntegrationSettings;
                Logger = logger;
            }

            /// <summary>
            /// Gets the order that has been sent to the ERP.
            /// </summary>
            /// <value>The order.</value>
            public Ecommerce.Orders.Order Order { get; }

            /// <summary>
            /// Shipping fee sent in the request.
            /// </summary>
            /// <value>Shipping fee sent in the request.</value>
            public PriceInfo ShippingFeeSentInRequest { get; }

            /// <summary>
            /// Gets the response XML order node.
            /// </summary>
            /// <value>The response XML order node.</value>
            public XmlNode OrderNode { get; }

            /// <summary>
            /// Set to True if standard Live integration Dynamicweb shipping processing is not needed.
            /// </summary>
            /// <value>True if standard Live integration Dynamicweb shipping processing is not needed.</value>
            public bool StopDefaultDynamicwebShippingProcessing { get; set; }

            /// <summary>
            /// Settings
            /// </summary>
            public Settings Settings { get; }

            /// <summary>
            /// Logger
            /// </summary>
            public Logger Logger { get; }
        }
    }
}