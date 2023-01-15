using Dynamicweb.Extensibility.Notifications;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples.Notifications
{
    /// <summary>
    /// Class OrderAfterSendToErpSubscriber.
    /// </summary>
    /// <seealso cref="NotificationSubscriber" />
    [Subscribe(Order.OnAfterSendingOrderToErp)]
    public class OrderAfterSendToErpSubscriber : NotificationSubscriber
    {
        /// <summary>
        /// Call to invoke observer.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="args">The args.</param>
        public override void OnNotify(string notification, NotificationArgs args)
        {
            var myArgs = (Order.OnAfterSendingOrderToErpArgs)args;

            // TODO: Add code here
            if (myArgs?.Error != null)
            {
                // Error occurred while sending order to ERP
            }
        }
    }
}
