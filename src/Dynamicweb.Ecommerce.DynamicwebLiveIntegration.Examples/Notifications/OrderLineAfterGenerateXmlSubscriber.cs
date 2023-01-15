using Dynamicweb.Extensibility.Notifications;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples.Notifications
{    
    /// <summary>
    /// Class OrderLineAfterGenerateXmlSubscriber.
    /// </summary>
    /// <seealso cref="NotificationSubscriber" />
    [Subscribe(OrderLine.OnAfterGenerateOrderLineXml)]
    public class OrderLineAfterGenerateXmlSubscriber : NotificationSubscriber
    {
        /// <summary>
        /// Call to invoke observer.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="args">The args.</param>
        public override void OnNotify(string notification, NotificationArgs args)
        {
            var myArgs = (OrderLine.OnAfterGenerateOrderLineXmlArgs)args;

            // TODO: Add code here
            if (myArgs?.OrderLineNode != null)
            {
                myArgs.Logger.Log(Logging.ErrorLevel.DebugInfo, "OrderLineAfterGenerateXmlSubscriber:[" + myArgs.OrderLineNode.OuterXml + "]");
            }
        }
    }
}
