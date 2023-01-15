using Dynamicweb.Environment;
using Dynamicweb.Extensibility.Notifications;
using System;
using System.Text;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators.IntegrationCustomerCenter
{
    /// <summary>
    /// Generates XML for integration customer center reports in pdf.
    /// </summary>
    /// <seealso cref="XmlGenerator" />
    internal class RetrievePdfXmlGenerator : XmlGenerator
    {
        /// <summary>
        /// Generates retrieve pdf xml
        /// </summary>
        /// <param name="settings">RetrievePdfXmlGeneratorSettings</param>        
        /// <returns></returns>
        internal string GenerateXml(RetrievePdfXmlGeneratorSettings settings)
        {
            NotificationManager.Notify(Notifications.IntegrationCustomerCenter.OnBeforeGenerateRetrievePdfXml,
                new Notifications.IntegrationCustomerCenter.OnBeforeGenerateRetrievePdfXmlArgs(settings));

            var xmlDocument = BuildXmlDocument();
            var requestElement = GetRequestElement(xmlDocument, settings.Type, settings.CustomerId, settings.ItemId);
            xmlDocument.AppendChild(requestElement);

            NotificationManager.Notify(Notifications.IntegrationCustomerCenter.OnAfterGenerateRetrievePdfXml,
                new Notifications.IntegrationCustomerCenter.OnAfterGenerateRetrievePdfXmlArgs(settings, xmlDocument));

            return xmlDocument.InnerXml;
        }

        private XmlElement GetRequestElement(XmlDocument xmlDocument, string itemType, string customerId, string itemId)
        {
            XmlElement result = xmlDocument.CreateElement("GetPDFForItem");
            result.SetAttribute("type", itemType);
            result.SetAttribute("id", itemId);
            result.SetAttribute("externalUserID", customerId);
            return result;
        }
    }
}