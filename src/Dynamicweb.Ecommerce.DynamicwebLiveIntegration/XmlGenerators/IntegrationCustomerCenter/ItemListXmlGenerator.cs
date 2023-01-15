using Dynamicweb.Environment;
using Dynamicweb.Extensibility.Notifications;
using System;
using System.Text;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators.IntegrationCustomerCenter
{
    /// <summary>
    /// Generates XML for integration customer center orders.
    /// </summary>
    /// <seealso cref="XmlGenerator" />
    internal class ItemListXmlGenerator : XmlGenerator
    {
        /// <summary>
        /// Generates orders list xml
        /// </summary>
        /// <param name="settings">ItemListXmlGeneratorSettings</param>        
        /// <returns></returns>
        internal string GenerateItemListXml(ItemListXmlGeneratorSettings settings)
        {
            NotificationManager.Notify(Notifications.IntegrationCustomerCenter.OnBeforeGenerateItemListXml,
                new Notifications.IntegrationCustomerCenter.OnBeforeGenerateItemListXmlArgs(settings));

            var xmlDocument = BuildXmlDocument();
            var requestElement = GetRequestElement(xmlDocument, settings.ItemType, settings.CustomerId, settings.PageSize, settings.PageIndex);
            xmlDocument.AppendChild(requestElement);

            NotificationManager.Notify(Notifications.IntegrationCustomerCenter.OnAfterGenerateItemListXml,
                new Notifications.IntegrationCustomerCenter.OnAfterGenerateItemListXmlArgs(settings, xmlDocument));

            return xmlDocument.InnerXml;
        }

        private XmlElement GetRequestElement(XmlDocument xmlDocument, string callType, string customerId, int pageSize, int pageIndex)
        {
            XmlElement result = xmlDocument.CreateElement("GetList");        
            result.SetAttribute("type", callType);
            result.SetAttribute("customerID", customerId);

            int firstItem = pageSize * pageIndex - pageSize + 1;
            if (firstItem <= 0)
                firstItem = 1;

            result.SetAttribute("requestAmount", pageSize.ToString());
            result.SetAttribute("firstItem", firstItem.ToString());

            if (!string.IsNullOrEmpty(Context.Current.Request.GetString("ICCSortByField")))
            {
                Enum.TryParse<SortDirection>(Context.Current.Request.GetString("ICCSortByDirection"), out SortDirection sortDirection);
                result.SetAttribute("sortByField", Context.Current.Request.GetString("ICCSortByField"));
                result.SetAttribute("sortDirection", sortDirection.ToString());
            }

            if (!string.IsNullOrEmpty(Context.Current.Request.GetString("ICCSearchField")) && !string.IsNullOrEmpty(Context.Current.Request.GetString("ICCSearchValue")))
            {
                result.SetAttribute("searchField", Context.Current.Request.GetString("ICCSearchField"));
                result.SetAttribute("searchValue", Context.Current.Request.GetString("ICCSearchValue"));
            }

            foreach (string key in Context.Current.Request.QueryString.AllKeys)
            {
                if (!string.IsNullOrEmpty(Context.Current.Request.GetString(key)) && key.ToLower().StartsWith("icc_"))
                {
                    result.SetAttribute(key.Substring(4), Context.Current.Request.GetString(key));
                }
            }

            foreach (string key in Context.Current.Request.Form.AllKeys)
            {
                if (!string.IsNullOrEmpty(Context.Current.Request.GetString(key)) && key.ToLower().StartsWith("icc_"))
                {
                    result.SetAttribute(key.Substring(4), Context.Current.Request.GetString(key));
                }
            }

            return result;
        }
    }
}