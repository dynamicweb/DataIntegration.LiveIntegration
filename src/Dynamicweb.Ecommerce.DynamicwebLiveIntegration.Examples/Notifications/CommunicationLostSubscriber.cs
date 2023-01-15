using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications;
using Dynamicweb.Extensibility.Notifications;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples.Notifications
{
    /// <summary>
    /// Class CommunicationLostSubscriber.
    /// </summary>
    /// <seealso cref="NotificationSubscriber" />
    [Subscribe(Communication.OnErpCommunicationLost)]
    public class CommunicationLostSubscriber : NotificationSubscriber
    {
        /// <summary>
        /// Call to invoke observer.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="args">The args.</param>
        public override void OnNotify(string notification, NotificationArgs args)
        {
            var myArgs = (Communication.OnErpCommunicationLostArgs)args;

            if (myArgs?.Exception != null)
            {
                myArgs.Logger.Log(Logging.ErrorLevel.DebugInfo, "Connection lost");
                myArgs.Logger.Log(Logging.ErrorLevel.ResponseError, myArgs.Exception.Message);
            }
        }
    }
}