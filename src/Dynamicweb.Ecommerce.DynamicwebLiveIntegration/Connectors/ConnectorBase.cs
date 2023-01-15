using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.EndpointMonitoring;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Licensing;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Extensibility.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using static Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.Communication;
namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors
{
    /// <summary>
    /// Abstract base connector
    /// </summary>
    internal abstract class ConnectorBase
    {
        protected Settings Settings { get; }
        protected Logger Logger { get; }
        public ConnectorBase(Settings settings, Logger logger)
        {
            Settings = settings;
            Logger = logger;
        }
        
        internal abstract EndpointInfo GetEndpoint();
        internal abstract string Execute(string request);
        internal abstract string Execute(EndpointInfo endpoint, string request, TimeSpan responseTimeout);

        internal abstract bool IsConnectionAvailableFromBackend(string endpointId, out string error);

        /// <summary>
        /// Determines whether the web service connection is available.
        /// </summary>
        internal abstract bool IsWebServiceConnectionAvailable();

        internal abstract IEnumerable<string> GetUrls(string multipleUrlsText);        

        internal void Error(EndpointInfo endpoint)
        {
            EndpointMonitoringService.Error(Settings, endpoint);
        }

        internal string Execute(EndpointInfo endpoint, string request)
        {
            return Execute(endpoint, request, TimeSpan.Zero);
        }

        protected bool ExecuteConnectionAvailableRequest(EndpointInfo endpoint, out Exception error)
        {
            error = null;
            if (EndpointMonitoringService.IsStillValid(Settings, endpoint, out bool connectionAvailable))
            {
                return connectionAvailable;
            }            
            Diagnostics.ExecutionTable.Current?.Add("DynamicwebLiveIntegration.ConnectorBase.IsWebServiceConnectionAvailable(endpoint) START");
            var lastStatus = EndpointMonitoringService.GetEndpointStatus(endpoint);
            bool isWebServiceConnectionAvailable = (lastStatus != null) && !lastStatus.Error;
            DateTime? lastErpCommunication = lastStatus?.LastErpCommunication;
            bool success = false;

            string request = "<GetEcomData></GetEcomData>"; // simple request for checking connection
            string response = null;
            try
            {
                TimeSpan ts = Settings.ConnectionTimeout > 0 ? new TimeSpan(0, 0, Settings.ConnectionTimeout) : TimeSpan.Zero;                
                response = Execute(endpoint, request, ts);
                success = true;
                EndpointMonitoringService.Success(endpoint);
            }
            catch (Exception ex)
            {
                error = ex;                
                Logger?.Log(ErrorLevel.ConnectionError, $"Error checking Web Service connection: '{ex.Message}'. Request: '{request}'.");

                EndpointMonitoringService.Error(Settings, endpoint);

                NotificationManager.Notify(OnErpCommunicationLost, new OnErpCommunicationLostArgs(request, ex, Settings, Logger));

                if (Connector.EnableThrowExceptions)
                {
                    throw new Exception("Thrown exception", ex);
                }
            }
            if (success && lastErpCommunication.HasValue && !isWebServiceConnectionAvailable)
            {
                NotificationManager.Notify(OnErpCommunicationRestored, new OnErpCommunicationRestoredArgs(request, lastErpCommunication, Settings, Logger));
            }
            if (success && !string.IsNullOrEmpty(response))
            {             
                LicenseService.ValidateLicense(endpoint.GetUrl(), response, Logger);
            }
            Diagnostics.ExecutionTable.Current?.Add("DynamicwebLiveIntegration.ConnectorBase.IsWebServiceConnectionAvailable(endpoint) END");
            return success;
        }

        internal bool IsConnectionAvailableFromBackend(string multipleUrlsText)
        {
            bool result = false;
            List<string> errorsList = new List<string>();
            IEnumerable<string> urls = null;
            if (!string.IsNullOrEmpty(multipleUrlsText))
            {
                result = true;
                urls = GetUrls(multipleUrlsText);
                foreach (string endpointId in urls)
                {
                    bool isAvailable = IsConnectionAvailableFromBackend(endpointId, out string message);
                    if (isAvailable)
                    {
                        result = result != false;
                    }
                    else
                    {
                        errorsList.Add(message);
                        result = false;
                    }
                }
            }
            if (!result && urls?.Count() > 1 && errorsList.Count > 0)
            {
                throw new Exception(string.Join(System.Environment.NewLine, errorsList));
            }
            return result;
        }
    }
}
