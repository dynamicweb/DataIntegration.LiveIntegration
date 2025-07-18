using Dynamicweb.Extensibility.Notifications;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples.Notifications
{
    /// <summary>
    /// Class ProductInfoOnAfterProcessResponseSubscriber.
    /// </summary>
    /// <seealso cref="NotificationSubscriber" />
    [Subscribe(ProductInfo.OnAfterProductInfoProcessResponse)]
    public class ProductInfoOnAfterProcessResponseSubscriber : NotificationSubscriber
    {
        /// <summary>
        /// Call to invoke observer.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="args">The args.</param>
        public override void OnNotify(string notification, NotificationArgs args)
        {
            var myArgs = (ProductInfo.OnAfterProductInfoProcessResponseArgs)args;

            // TODO: Add code here
            if (myArgs?.ProductInfo != null)
            {

            }
        }
    }
}
