using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators;
using Dynamicweb.Extensibility.Notifications;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications
{   
    /// <summary>
    /// Notifications and argument classes for notification subscribers fired during generation of order line XML.
    /// </summary>
    public static class OrderLine
    {
        /// <summary>
        /// Occurs before the XML for an order line is generated. This enables you to change or analyze the order line before it's processed.
        /// </summary>
        /// <example>
        /// <code description="Notification observer example" source="..\..\Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples\Notifications\OrderLineBeforeGenerateXmlSubscriber.cs" lang="CS"></code>
        /// </example>
        public const string OnBeforeGenerateOrderLineXml = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnBeforeGenerateOrderLineXml";

        /// <summary>
        /// Occurs after the XML for an order line is generated. This enables you to change or analyze the XML before it's sent to the ERP.
        /// </summary>
        /// <example>
        /// <code description="Notification observer example" source="..\..\Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples\Notifications\OrderLineAfterGenerateXmlSubscriber.cs" lang="CS"></code>
        /// </example>
        public const string OnAfterGenerateOrderLineXml = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnAfterGenerateOrderLineXml";

        /// <summary>
        /// Arguments class for the OnBeforeGenerateOrderLineXml subscriber.
        /// </summary>
        /// <seealso cref="NotificationArgs" />
        public class OnBeforeGenerateOrderLineXmlArgs : NotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnBeforeGenerateOrderLineXmlArgs"/> class.
            /// </summary>
            /// <param name="orderLine">The order line for which the XML is being generated.</param>
            /// <param name="settings">The settings used for generating the XML.</param>
            public OnBeforeGenerateOrderLineXmlArgs(Ecommerce.Orders.OrderLine orderLine, OrderXmlGeneratorSettings settings, Settings liveIntegrationSettings, Logger logger)
            {
                OrderLine = orderLine;
                GeneratorSettings = settings;
                Settings = liveIntegrationSettings;
                Logger = logger;
            }

            /// <summary>
            /// Gets the order line for which the XML is being generated.
            /// </summary>
            /// <value>The order line.</value>
            public Ecommerce.Orders.OrderLine OrderLine { get; }

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
        /// Arguments class for the OnAfterGenerateOrderLineXml subscriber.
        /// </summary>
        /// <seealso cref="NotificationArgs" />
        public class OnAfterGenerateOrderLineXmlArgs : NotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnAfterGenerateOrderLineXmlArgs"/> class.
            /// </summary>
            /// <param name="orderLine">The order line for which the XML has been generated.</param>
            /// <param name="orderLineNode">The XML node that has been created. You can manipulate this node to alter the XML being sent to the ERP.</param>
            public OnAfterGenerateOrderLineXmlArgs(Ecommerce.Orders.OrderLine orderLine, XmlNode orderLineNode, Settings liveIntegrationSettings, Logger logger)
            {
                OrderLine = orderLine;
                OrderLineNode = orderLineNode;
                Settings = liveIntegrationSettings;
                Logger = logger;
            }

            /// <summary>
            /// Gets the order line for which the XML has been generated..
            /// </summary>
            /// <value>The order line.</value>
            public Ecommerce.Orders.OrderLine OrderLine { get; }

            /// <summary>
            /// Gets the XML node that has been created. You can manipulate this node to alter the XML being sent to the ERP.
            /// </summary>
            /// <value>The order line node.</value>
            public XmlNode OrderLineNode { get; }

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