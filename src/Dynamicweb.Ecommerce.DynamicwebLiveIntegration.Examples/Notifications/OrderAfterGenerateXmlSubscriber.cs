using Dynamicweb.Extensibility.Notifications;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples.Notifications
{
    /// <summary>
    /// Class OrderAfterGenerateXmlSubscriber.
    /// </summary>
    /// <seealso cref="NotificationSubscriber" />
    [Subscribe(Order.OnAfterGenerateOrderXml)]
    public class OrderAfterGenerateXmlSubscriber : NotificationSubscriber
    {
        /// <summary>
        /// Call to invoke observer.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="args">The args.</param>
        public override void OnNotify(string notification, NotificationArgs args)
        {
            var myArgs = (Order.OnAfterGenerateOrderXmlArgs)args;

            // TODO: Add code here
            if (myArgs?.Document != null)
            {
                var settings = SettingsManager.GetSettingsByShop(myArgs.Order.ShopId);
                if (settings != null && !settings.ErpControlsShipping)
                {
                    var order = myArgs.Order;
                    var shipping = Services.Shippings.GetShipping(order.ShippingMethodId);
                    if (shipping != null)
                    {
                        var itemNode = myArgs.Document.SelectSingleNode("//item [@table='EcomOrders']");
                        if (itemNode != null)
                        {
                            AddChildXmlNode(itemNode, "OrderShippingCode", shipping.Code);
                            AddChildXmlNode(itemNode, "OrderShippingAgentCode", shipping.AgentCode);
                            AddChildXmlNode(itemNode, "OrderShippingAgentName", shipping.GetAgentName(order.LanguageId));
                            AddChildXmlNode(itemNode, "OrderShippingAgentServiceCode", shipping.AgentServiceCode);
                            AddChildXmlNode(itemNode, "OrderShippingAgentServiceDescription", shipping.GetAgentServiceDescription(order.LanguageId));
                        }
                    }
                }
            }
        }

        private void AddChildXmlNode(XmlNode parent, string nodeName, string nodeValue)
        {
            var node = parent.OwnerDocument.CreateElement("column");
            node.SetAttribute("columnName", nodeName);
            node.InnerText = nodeValue;
            parent.AppendChild(node);
        }
    }
}
