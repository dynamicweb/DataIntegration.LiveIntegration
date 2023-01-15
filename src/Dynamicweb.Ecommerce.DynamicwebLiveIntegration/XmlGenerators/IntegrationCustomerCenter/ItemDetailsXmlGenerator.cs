using Dynamicweb.Environment;
using Dynamicweb.Extensibility.Notifications;
using System;
using System.Text;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators.IntegrationCustomerCenter
{
    /// <summary>
    /// Generates XML for integration customer center order details.
    /// </summary>
    /// <seealso cref="XmlGenerator" />
    internal class ItemDetailsXmlGenerator : XmlGenerator
    {
        /// <summary>
        /// Generates orders details xml
        /// </summary>
        /// <param name="settings">ItemDetailsXmlGeneratorSettings</param>        
        /// <returns></returns>
        internal string GenerateItemDetailsXml(ItemDetailsXmlGeneratorSettings settings)
        {            
            NotificationManager.Notify(Notifications.IntegrationCustomerCenter.OnBeforeGenerateItemDetailsXml,
                new Notifications.IntegrationCustomerCenter.OnBeforeGenerateItemDetailsXmlArgs(settings));

            var xmlDocument = BuildXmlDocument();
            var requestElement = GetRequestElement(xmlDocument, settings.ItemType, settings.CustomerId, settings.ItemId);
            xmlDocument.AppendChild(requestElement);

            NotificationManager.Notify(Notifications.IntegrationCustomerCenter.OnAfterGenerateItemDetailsXml,
                new Notifications.IntegrationCustomerCenter.OnAfterGenerateItemDetailsXmlArgs(settings, xmlDocument));

            return xmlDocument.InnerXml;
        }

        private XmlElement GetRequestElement(XmlDocument xmlDocument, string itemType, string customerId, string itemId)
        {
            XmlElement result = xmlDocument.CreateElement("GetItem");
            result.SetAttribute("type", itemType);
            result.SetAttribute("customerID", customerId);
            result.SetAttribute("documentNO", itemId);                        
            return result;
        }
    }
}