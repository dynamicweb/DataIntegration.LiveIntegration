using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators.IntegrationCustomerCenter;
using Dynamicweb.Extensibility.Notifications;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications
{
    /// <summary>
    /// Notifications and argument classes for notification subscribers fired during generation of Integration Customer Center XMLs.
    /// </summary>
    public static class IntegrationCustomerCenter
    {
        /// <summary>
        /// Occurs before the XML for an order list generated. This enables you to change or analyze the xml before it's processed.
        /// </summary>
        public const string OnBeforeGenerateItemListXml = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnBeforeGenerateItemListXml";

        /// <summary>
        /// Arguments class for the OnBeforeGenerateItemListXml subscriber.
        /// </summary>
        /// <seealso cref="NotificationArgs" />
        public class OnBeforeGenerateItemListXmlArgs : NotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnBeforeGenerateItemListXmlArgs"/> class.
            /// </summary>            
            /// <param name="settings">An instance of ItemListXmlGeneratorSettings that determines how the XML is generated.</param>
            public OnBeforeGenerateItemListXmlArgs(ItemListXmlGeneratorSettings settings)
            {
                ItemListXmlGeneratorSettings = settings;
            }

            /// <summary>
            /// Gets the ItemListXmlGeneratorSettings that determines how the XML is generated
            /// </summary>
            /// <value>The generator settings.</value>
            public ItemListXmlGeneratorSettings ItemListXmlGeneratorSettings;            
        }

        /// <summary>
        /// Occurs after the XML for an order list generated. This enables you to change or analyze the xml before it's processed.
        /// </summary>
        public const string OnAfterGenerateItemListXml = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnAfterGenerateItemListXml";

        /// <summary>
        /// Arguments class for the OnAfterGenerateItemListXml subscriber.
        /// </summary>
        /// <seealso cref="NotificationArgs" />
        public class OnAfterGenerateItemListXmlArgs : NotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnAfterGenerateItemListXmlArgs"/> class.
            /// </summary>            
            /// <param name="settings">The settings used for generating the XML.</param>
            /// <param name="xmlDocument">The XML document that has been created. You can manipulate this document to alter the XML being sent to the ERP.</param>
            public OnAfterGenerateItemListXmlArgs(ItemListXmlGeneratorSettings settings, XmlDocument xmlDocument)
            {
                ItemListXmlGeneratorSettings = settings;
                Document = xmlDocument;
            }

            /// <summary>
            /// Gets the generator settings.
            /// </summary>
            /// <value>The generator settings.</value>
            public ItemListXmlGeneratorSettings ItemListXmlGeneratorSettings;

            /// <summary>
            /// Gets the XML document that has been created. You can manipulate this document to alter the XML being sent to the ERP..
            /// </summary>
            /// <value>The document.</value>
            public XmlDocument Document;
        }

        /// <summary>
        /// Occurs before the XML for an order details generated. This enables you to change or analyze the xml before it's processed.
        /// </summary>
        public const string OnBeforeGenerateItemDetailsXml = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnBeforeGenerateItemDetailsXml";

        /// <summary>
        /// Arguments class for the OnBeforeGenerateItemDetailsXml subscriber.
        /// </summary>
        /// <seealso cref="NotificationArgs" />
        public class OnBeforeGenerateItemDetailsXmlArgs : NotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnBeforeGenerateItemDetailsXmlArgs"/> class.
            /// </summary>            
            /// <param name="settings">An instance of ItemDetailsXmlGeneratorSettings that determines how the XML is generated.</param>
            public OnBeforeGenerateItemDetailsXmlArgs(ItemDetailsXmlGeneratorSettings settings)
            {
                ItemDetailsXmlGeneratorSettings = settings;
            }

            /// <summary>
            /// Gets the ItemDetailsXmlGeneratorSettings that determines how the XML is generated
            /// </summary>
            /// <value>The generator settings.</value>
            public ItemDetailsXmlGeneratorSettings ItemDetailsXmlGeneratorSettings;
        }

        /// <summary>
        /// Occurs after the XML for an order details generated. This enables you to change or analyze the xml before it's processed.
        /// </summary>
        public const string OnAfterGenerateItemDetailsXml = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnAfterGenerateItemDetailsXml";

        /// <summary>
        /// Arguments class for the OnAfterGenerateItemDetailsXml subscriber.
        /// </summary>
        /// <seealso cref="NotificationArgs" />
        public class OnAfterGenerateItemDetailsXmlArgs : NotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnAfterGenerateItemDetailsXmlArgs"/> class.
            /// </summary>            
            /// <param name="settings">The settings used for generating the XML.</param>
            /// <param name="xmlDocument">The XML document that has been created. You can manipulate this document to alter the XML being sent to the ERP.</param>
            public OnAfterGenerateItemDetailsXmlArgs(ItemDetailsXmlGeneratorSettings settings, XmlDocument xmlDocument)
            {
                ItemDetailsXmlGeneratorSettings = settings;
                Document = xmlDocument;
            }

            /// <summary>
            /// Gets the generator settings.
            /// </summary>
            /// <value>The generator settings.</value>
            public ItemDetailsXmlGeneratorSettings ItemDetailsXmlGeneratorSettings;

            /// <summary>
            /// Gets the XML document that has been created. You can manipulate this document to alter the XML being sent to the ERP..
            /// </summary>
            /// <value>The document.</value>
            public XmlDocument Document;
        }

        /// <summary>
        /// Occurs before the XML for the retrieve pdf generated. This enables you to change or analyze the xml before it's processed.
        /// </summary>
        public const string OnBeforeGenerateRetrievePdfXml = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnBeforeGenerateRetrievePdfXml";

        /// <summary>
        /// Arguments class for the OnBeforeGenerateRetrievePdfXml subscriber.
        /// </summary>
        /// <seealso cref="NotificationArgs" />
        public class OnBeforeGenerateRetrievePdfXmlArgs : NotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnBeforeGenerateRetrievePdfXmlArgs"/> class.
            /// </summary>            
            /// <param name="settings">An instance of RetrievePdfXmlGeneratorSettings that determines how the XML is generated.</param>
            public OnBeforeGenerateRetrievePdfXmlArgs(RetrievePdfXmlGeneratorSettings settings)
            {
                RetrievePdfXmlGeneratorSettings = settings;
            }

            /// <summary>
            /// Gets the RetrievePdfXmlGeneratorSettings that determines how the XML is generated
            /// </summary>
            /// <value>The generator settings.</value>
            public RetrievePdfXmlGeneratorSettings RetrievePdfXmlGeneratorSettings;
        }

        /// <summary>
        /// Occurs after the XML for the retrieve pdf generated. This enables you to change or analyze the xml before it's processed.
        /// </summary>
        public const string OnAfterGenerateRetrievePdfXml = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnAfterGenerateRetrievePdfXml";

        /// <summary>
        /// Arguments class for the OnAfterGenerateItemDetailsXml subscriber.
        /// </summary>
        /// <seealso cref="NotificationArgs" />
        public class OnAfterGenerateRetrievePdfXmlArgs : NotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnAfterGenerateRetrievePdfXmlArgs"/> class.
            /// </summary>            
            /// <param name="settings">The settings used for generating the XML.</param>
            /// <param name="xmlDocument">The XML document that has been created. You can manipulate this document to alter the XML being sent to the ERP.</param>
            public OnAfterGenerateRetrievePdfXmlArgs(RetrievePdfXmlGeneratorSettings settings, XmlDocument xmlDocument)
            {
                RetrievePdfXmlGeneratorSettings = settings;
                Document = xmlDocument;
            }

            /// <summary>
            /// Gets the generator settings.
            /// </summary>
            /// <value>The generator settings.</value>
            public RetrievePdfXmlGeneratorSettings RetrievePdfXmlGeneratorSettings;

            /// <summary>
            /// Gets the XML document that has been created. You can manipulate this document to alter the XML being sent to the ERP..
            /// </summary>
            /// <value>The document.</value>
            public XmlDocument Document;
        }
    }
}
