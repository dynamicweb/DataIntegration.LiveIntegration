using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications;
using Dynamicweb.Extensibility.Notifications;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples.Notifications
{
    /// <summary>
    /// Class OrderOnBeforeUpdateDynamicwebShippingSubscriber.
    /// </summary>
    /// <seealso cref="NotificationSubscriber" />
    [Subscribe(Order.OnBeforeUpdateDynamicwebShipping)]
    public class OrderOnBeforeUpdateDynamicwebShippingSubscriber : NotificationSubscriber
    {
        /// <summary>
        /// Call to invoke observer.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="args">The args.</param>
        public override void OnNotify(string notification, NotificationArgs args)
        {
            var myArgs = (Order.OnBeforeUpdateDynamicwebShippingArgs)args;

            // TODO: Add code here
            if (myArgs?.Order != null)
            {
                myArgs.StopDefaultDynamicwebShippingProcessing = true;
            }
        }
    }
}
