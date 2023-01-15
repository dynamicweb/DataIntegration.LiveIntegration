using Dynamicweb.Extensibility.Notifications;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples.Notifications
{    
    /// <summary>
    /// Class CommunicationRestoredSubscriber.
    /// </summary>
    /// <seealso cref="NotificationSubscriber" />
    [Subscribe(Communication.OnErpCommunicationRestored)]
    public class CommunicationRestoredSubscriber : NotificationSubscriber
    {
        /// <summary>
        /// Call to invoke observer.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="args">The args.</param>
        public override void OnNotify(string notification, NotificationArgs args)
        {
            var myArgs = (Communication.OnErpCommunicationRestoredArgs)args;

            // TODO: Add code here
            if (myArgs != null)
            {
                myArgs.Logger.Log(Logging.ErrorLevel.DebugInfo, "Connection restored");
            }
        }
    }
}
