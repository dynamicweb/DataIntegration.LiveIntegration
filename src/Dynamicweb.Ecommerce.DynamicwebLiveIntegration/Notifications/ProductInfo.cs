using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators;
using Dynamicweb.Ecommerce.Products;
using Dynamicweb.Extensibility.Notifications;
using System.Collections.Generic;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications
{
    /// <summary>
    /// Notifications and argument classes for notification subscribers fired during generation of product information XML.
    /// </summary>
    public static class ProductInfo
    {
        /// <summary>
        /// Occurs before the XML for a product info request is generated. This enables you to change or analyze the product before it's processed.
        /// </summary>
        /// <example>
        /// <code description="Notification observer example" source="..\..\source="..\..\Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples\Notifications\OrderAfterGenerateXmlSubscriber.cs"Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples\Notifications\ProductInfoBeforeGenerateXmlSubscriber.cs" lang="CS"></code>
        /// </example>
        public const string OnBeforeGenerateProductInfoXml = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnBeforeGenerateProductInfoXml";

        /// <summary>
        /// Occurs after the XML for a product info request is generated. This enables you to change or analyze the XML before it's sent to the ERP.
        /// </summary>
        /// <example>
        /// <code description="Notification observer example" source="..\..\source="..\..\Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples\Notifications\OrderAfterGenerateXmlSubscriber.cs"Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples\Notifications\ProductInfoAfterGenerateXmlSubscriber.cs" lang="CS"></code>
        /// </example>
        public const string OnAfterGenerateProductInfoXml = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnAfterGenerateProductInfoXml";

        /// <summary>
        /// Arguments class for the OnBeforeGenerateProductInfoXml subscriber.
        /// </summary>
        /// <seealso cref="NotificationArgs" />
        public class OnBeforeGenerateProductInfoXmlArgs : NotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnBeforeGenerateProductInfoXmlArgs"/> class.
            /// </summary>
            /// <param name="products">The products for which the XML is generated.</param>
            /// <param name="settings">An instance of ProductInfoXmlGeneratorSettings that determines how the XML is generated.</param>
            public OnBeforeGenerateProductInfoXmlArgs(Dictionary<Product, double> products, ProductInfoXmlGeneratorSettings settings, Settings liveIntegrationSettings, Logger logger)
            {
                Products = products;
                GeneratorSettings = settings;
                Settings = liveIntegrationSettings;
                Logger = logger;
            }

            /// <summary>
            /// Gets the products for which the XML is being generated..
            /// </summary>
            /// <value>The products.</value>
            public Dictionary<Product, double> Products { get; }

            /// <summary>
            /// Gets the ProductInfoXmlGeneratorSettings that determines how the XML is generated
            /// </summary>
            /// <value>The generator settings.</value>
            public ProductInfoXmlGeneratorSettings GeneratorSettings { get; }

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
        /// Arguments class for the OnAfterGenerateProductInfoXml subscriber.
        /// </summary>
        /// <seealso cref="NotificationArgs" />
        public class OnAfterGenerateProductInfoXmlArgs : NotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnAfterGenerateProductInfoXmlArgs"/> class.
            /// </summary>
            /// <param name="products">The products for which the XML has been generated.</param>
            /// <param name="settings">The ProductInfoXmlGeneratorSettings that determines how the XML is generated.</param>
            /// <param name="xmlDocument">The XML document that has been created. You can manipulate this document to alter the XML being sent to the ERP.</param>
            public OnAfterGenerateProductInfoXmlArgs(Dictionary<Product, double> products, ProductInfoXmlGeneratorSettings settings, XmlDocument xmlDocument, Settings liveIntegrationSettings, Logger logger)
            {
                Products = products;
                GeneratorSettings = settings;
                XmlDocument = xmlDocument;
                Settings = liveIntegrationSettings;
                Logger = logger;
            }

            /// <summary>
            /// Gets the products for which the XML has been generated.
            /// </summary>
            /// <value>The products.</value>
            public Dictionary<Product, double> Products { get; }

            /// <summary>
            /// Gets the ProductInfoXmlGeneratorSettings that determines how the XML is generated.
            /// </summary>
            /// <value>The generator settings.</value>
            public ProductInfoXmlGeneratorSettings GeneratorSettings { get; }

            /// <summary>
            /// Gets the XML document that has been created. You can manipulate this document to alter the XML being sent to the ERP.
            /// </summary>
            /// <value>The XML document.</value>
            public XmlDocument XmlDocument { get; }

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