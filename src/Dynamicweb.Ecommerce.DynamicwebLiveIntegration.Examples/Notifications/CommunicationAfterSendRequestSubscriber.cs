using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications;
using Dynamicweb.Extensibility.Notifications;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples.Notifications
{
    /// <summary>
    /// Class CommunicationAfterSendRequestSubscriber.
    /// </summary>
    /// <seealso cref="NotificationSubscriber" />
    [Subscribe(Communication.OnAfterErpCommunication)]
    public class CommunicationAfterSendRequestSubscriber : NotificationSubscriber
    {
        /// <summary>
        /// Call to invoke observer.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="args">The args.</param>
        public override void OnNotify(string notification, NotificationArgs args)
        {
            var myArgs = (Communication.OnAfterErpCommunicationArgs)args;

            if (!string.IsNullOrEmpty(myArgs?.Response))
            {
                myArgs.Logger.Log(Logging.ErrorLevel.DebugInfo, myArgs.Response);
            }
        }
    }
}