using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Extensibility.Notifications;
using System;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications
{
    /// <summary>
    /// Notifications and argument classes for notification subscribers fired during communication wth the ERP.
    /// </summary>
    public static class Communication
    {
        /// <summary>
        /// Occurs before the system communicates with the ERP.
        /// </summary>
        /// <example>
        /// <code description="Notification observer example" source="..\..\Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples\Notifications\CommunicationBeforeSendRequestSubscriber.cs" lang="CS"></code>
        /// </example>
        public const string OnBeforeErpCommunication = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnBeforeErpCommunication";

        /// <summary>
        /// Occurs after the system communicated with the ERP.
        /// </summary>
        /// <example>
        /// <code description="Notification observer example" source="..\..\Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples\Notifications\CommunicationAfterSendRequestSubscriber.cs" lang="CS"></code>
        /// </example>
        public const string OnAfterErpCommunication = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnAfterErpCommunication";

        /// <summary>
        /// Occurs after an exception has occurred communicating with the ERP.
        /// </summary>
        /// <example>
        /// <code description="Notification observer example" source="..\..\Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples\Notifications\CommunicationAfterErpExceptionSubscriber.cs" lang="CS"></code>
        /// </example>
        public const string OnAfterErpException = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnAfterErpException";

        /// <summary>
        /// Occurs after the connection with the ERP is lost.
        /// </summary>
        /// <example>
        /// <code description="Notification observer example" source="..\..\Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples\Notifications\CommunicationLostSubscriber.cs" lang="CS"></code>
        /// </example>
        public const string OnErpCommunicationLost = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnErpCommunicationLost";

        /// <summary>
        /// Occurs after the connection with the ERP is restored.
        /// </summary>
        /// <example>
        /// <code description="Notification observer example" source="..\..\Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples\Notifications\CommunicationRestoredSubscriber.cs" lang="CS"></code>
        /// </example>
        public const string OnErpCommunicationRestored = "Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.LiveIntegration.OnErpCommunicationRestored";

        /// <summary>
        /// Arguments class for the OnBeforeErpCommunication subscriber.
        /// </summary>
        /// <seealso cref="NotificationArgs" />
        public class OnBeforeErpCommunicationArgs : NotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnBeforeErpCommunicationArgs"/> class.
            /// </summary>
            /// <param name="request">The request that is about to be sent.</param>
            /// <param name="referenceName">Name of the reference.</param>
            public OnBeforeErpCommunicationArgs(string request, string referenceName, Settings settings, Logger logger)
            {
                Request = request;
                ReferenceName = referenceName;
                Settings = settings;
                Logger = logger;
            }

            /// <summary>
            /// Gets the request that is about to be sent.
            /// </summary>
            /// <value>The request.</value>
            public string Request { get; }

            /// <summary>
            /// Gets the name of the reference.
            /// </summary>
            /// <value>The name of the reference.</value>
            public string ReferenceName { get; }

            /// <summary>
            /// Settings
            /// </summary>
            public Settings Settings { get; }

            /// <summary>
            /// Logger
            /// </summary>
            public Logger Logger { get; }
        }

        /// <summary>
        /// Arguments class for the OnAfterErpCommunication subscriber.
        /// </summary>
        /// <seealso cref="NotificationArgs" />
        public class OnAfterErpCommunicationArgs : NotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnAfterErpCommunicationArgs"/> class.
            /// </summary>
            /// <param name="request">The request that has been sent.</param>
            /// <param name="response">The response that has been received.</param>
            /// <param name="referenceName">Name of the reference.</param>
            /// <param name="exception">The exception that has occurred.</param>
            public OnAfterErpCommunicationArgs(string request, string response, string referenceName, Exception exception, Settings settings, Logger logger)
            {
                Request = request;
                Response = response;
                ReferenceName = referenceName;
                Exception = exception;
                Settings = settings;
                Logger = logger;
            }

            /// <summary>
            /// Gets the request that has been sent to the ERP.
            /// </summary>
            /// <value>The request.</value>
            public string Request { get; }

            /// <summary>
            /// Gets the name of the reference.
            /// </summary>
            /// <value>The name of the reference.</value>
            public string ReferenceName { get; }

            /// <summary>
            /// Gets the response that has been received.
            /// </summary>
            /// <value>The response.</value>
            public string Response { get; }

            /// <summary>
            /// Gets the exception that has occurred.
            /// </summary>
            /// <value>The exception.</value>
            public Exception Exception { get; }

            /// <summary>
            /// Settings
            /// </summary>
            public Settings Settings { get; }

            /// <summary>
            /// Logger
            /// </summary>
            public Logger Logger { get; }
        }

        /// <summary>
        /// Arguments class for the OnAfterErpException subscriber.
        /// </summary>
        /// <seealso cref="NotificationArgs" />
        public class OnAfterErpExceptionArgs : NotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnAfterErpExceptionArgs"/> class.
            /// </summary>
            /// <param name="request">The request that has been sent.</param>
            /// <param name="response">The response that has been received.</param>
            /// <param name="referenceName">Name of the reference.</param>
            /// <param name="exception">The exception that has occurred.</param>
            public OnAfterErpExceptionArgs(string request, string response, string referenceName, Exception exception, Settings settings, Logger logger)
            {
                Request = request;
                Response = response;
                ReferenceName = referenceName;
                Exception = exception;
                Settings = settings;
                Logger = logger;
            }

            /// <summary>
            /// Gets the request that has been sent to the ERP.
            /// </summary>
            /// <value>The request.</value>
            public string Request { get; }

            /// <summary>
            /// Gets the name of the reference.
            /// </summary>
            /// <value>The name of the reference.</value>
            public string ReferenceName { get; }

            /// <summary>
            /// Gets the response that has been received.
            /// </summary>
            /// <value>The response.</value>
            public string Response { get; }

            /// <summary>
            /// Gets the exception that has occurred.
            /// </summary>
            /// <value>The exception.</value>
            public Exception Exception { get; }

            /// <summary>
            /// Settings
            /// </summary>
            public Settings Settings { get; }

            /// <summary>
            /// Logger
            /// </summary>
            public Logger Logger { get; }
        }

        /// <summary>
        /// Arguments class for the OnErpCommunicationLost subscriber.
        /// </summary>
        /// <seealso cref="NotificationArgs" />
        public class OnErpCommunicationLostArgs : NotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnErpCommunicationLostArgs"/> class.
            /// </summary>
            /// <param name="request">The request that has been sent.</param>
            /// <param name="exception">The exception that has occurred.</param>
            public OnErpCommunicationLostArgs(string request, Exception exception, Settings settings, Logger logger)
            {
                Request = request;
                Exception = exception;
                Settings = settings;
                Logger = logger;
            }

            /// <summary>
            /// Gets the request that has been sent to the ERP.
            /// </summary>
            /// <value>The request.</value>
            public string Request { get; }

            /// <summary>
            /// Gets the exception that has occurred.
            /// </summary>
            /// <value>The exception.</value>
            public Exception Exception { get; }

            /// <summary>
            /// Settings
            /// </summary>
            public Settings Settings { get; }

            /// <summary>
            /// Logger
            /// </summary>
            public Logger Logger { get; }
        }

        /// <summary>
        /// Arguments class for the OnErpCommunicationRestored subscriber.
        /// </summary>
        /// <seealso cref="NotificationArgs" />
        public class OnErpCommunicationRestoredArgs : NotificationArgs
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="OnErpCommunicationRestoredArgs"/> class.
            /// </summary>
            /// <param name="request">The request that has been sent.</param>
            /// <param name="lastErpCommunication">The date and time of the last successful communication with the ERP.</param>
            public OnErpCommunicationRestoredArgs(string request, DateTime? lastErpCommunication, Settings settings, Logger logger)
            {
                Request = request;
                LastErpCommunication = lastErpCommunication;
                Settings = settings;
                Logger = logger;
            }

            /// <summary>
            /// The request that has been sent.
            /// </summary>
            /// <value>The request.</value>
            public string Request { get; }

            /// <summary>
            /// The date and time of the last successful communication with the ERP.
            /// </summary>
            /// <value>The last erp communication.</value>
            public DateTime? LastErpCommunication { get; }

            /// <summary>
            /// Settings
            /// </summary>
            public Settings Settings { get; }

            /// <summary>
            /// Logger
            /// </summary>
            public Logger Logger { get; }
        }
    }
}