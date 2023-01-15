using Dynamicweb.Extensibility.Notifications;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples.Notifications.IntegrationCustomerCenter
{
    /// <summary>
    /// Class ItemDetailsAfterGenerateXmlSubscriber.
    /// </summary>
    /// <seealso cref="NotificationSubscriber" />
    [Subscribe(DynamicwebLiveIntegration.Notifications.IntegrationCustomerCenter.OnAfterGenerateItemDetailsXml)]

    public class ItemDetailsAfterGenerateXmlSubscriber : NotificationSubscriber
    {
        /// <summary>
        /// Call to invoke observer.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="args">The args.</param>
        public override void OnNotify(string notification, NotificationArgs args)
        {
            var myArgs = (DynamicwebLiveIntegration.Notifications.IntegrationCustomerCenter.OnAfterGenerateItemDetailsXmlArgs)args;

            // TODO: Add code here
            if (myArgs?.Document != null)
            {

            }
        }
    }
}
