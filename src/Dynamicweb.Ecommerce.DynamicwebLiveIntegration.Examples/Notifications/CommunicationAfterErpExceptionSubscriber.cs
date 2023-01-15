using Dynamicweb.Extensibility.Notifications;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples.Notifications
{    
    /// <summary>
    /// Class CommunicationAfterErpExceptionSubscriber.
    /// </summary>
    /// <seealso cref="NotificationSubscriber" />
    [Subscribe(Communication.OnAfterErpException)]
    public class CommunicationAfterErpExceptionSubscriber : NotificationSubscriber
    {
        /// <summary>
        /// Call to invoke observer.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="args">The args.</param>
        public override void OnNotify(string notification, NotificationArgs args)
        {
            var myArgs = (Communication.OnAfterErpExceptionArgs)args;

            // TODO: Add code here
            if (myArgs?.Exception != null)
            {                
                myArgs.Logger.Log(Logging.ErrorLevel.Error, "Some error occurred");
            }
        }
    }
}
