using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications;
using Dynamicweb.Extensibility.Notifications;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples.Notifications
{
    /// <summary>
    /// Class CommunicationBeforeSendRequestSubscriber.
    /// </summary>
    /// <seealso cref="NotificationSubscriber" />
    [Subscribe(Communication.OnBeforeErpCommunication)]
    public class CommunicationBeforeSendRequestSubscriber : NotificationSubscriber
    {
        /// <summary>
        /// Call to invoke observer.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="args">The args.</param>
        public override void OnNotify(string notification, NotificationArgs args)
        {
            var myArgs = (Communication.OnBeforeErpCommunicationArgs)args;

            if (!string.IsNullOrEmpty(myArgs?.Request))
            {
                myArgs.Logger.Log(Logging.ErrorLevel.DebugInfo, myArgs.Request);
            }
        }
    }
}