using Dynamicweb.Extensibility.Notifications;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples.Notifications
{    
    /// <summary>
    /// Class OrderBeforeGenerateXmlSubscriber.
    /// </summary>
    /// <seealso cref="NotificationSubscriber" />
    [Subscribe(Order.OnBeforeGenerateOrderXml)]
    public class OrderBeforeGenerateXmlSubscriber : NotificationSubscriber
    {
        /// <summary>
        /// Call to invoke observer.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="args">The args.</param>
        public override void OnNotify(string notification, NotificationArgs args)
        {
            var myArgs = (Order.OnBeforeGenerateOrderXmlArgs)args;

            // TODO: Add code here
            if (myArgs?.Order != null)
            {

            }
        }
    }
}
