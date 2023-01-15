using Dynamicweb.Extensibility.Notifications;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples.Notifications
{    
    /// <summary>
    /// Class OrderBeforeSendToErpSubscriber.
    /// </summary>
    /// <seealso cref="NotificationSubscriber" />
    [Subscribe(Order.OnBeforeSendingOrderToErp)]
    public class OrderBeforeSendToErpSubscriber : NotificationSubscriber
    {
        /// <summary>
        /// Call to invoke observer.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="args">The args.</param>
        public override void OnNotify(string notification, NotificationArgs args)
        {
            var myArgs = (Order.OnBeforeSendingOrderToErpArgs)args;

            // TODO: Add code here
            // Sample: when order price less than 1000 cancel order sending
            if (myArgs.CreateOrder && myArgs.Order.Price.PriceWithVAT < 1000)
            {
                myArgs.Cancel = true;
            }
        }
    }
}
