using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.EndpointMonitoring
{
    /// <summary>
    /// EndpointMonitoringService
    /// </summary>
    internal class EndpointMonitoringService
    {
        private static readonly ConcurrentDictionary<string, EndpointStatus> EndpointCollection = new ConcurrentDictionary<string, EndpointStatus>();
        private static readonly List<string> EndpointsInPing = new List<string>();
        private static readonly ConcurrentDictionary<string, Timer> PingTimers = new ConcurrentDictionary<string, Timer>();

        /// <summary>
        /// Checks if endpoint connection status is still valid
        /// </summary>        
        /// <param name="endpoint">The endpoint</param>
        /// <param name="connectionAvailable">connection available result</param>
        /// <returns>True if status is still valid otherwise false</returns>
        public static bool IsStillValid(Settings settings, EndpointInfo endpoint, out bool connectionAvailable)
        {
            connectionAvailable = false;

            if (EndpointsInPing.Contains(endpoint.Id))
            {
                return true;
            }
            if (EndpointCollection.TryGetValue(endpoint.GetUrl(), out EndpointStatus endpointStatus))
            {
                connectionAvailable = !endpointStatus.Error;
                return endpointStatus.IsStillValid(settings);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns current endpoint status
        /// </summary>        
        /// <param name="endpoint">The endpoint</param>
        /// <returns>Endpoint connection status if it exists otherwise null</returns>
        public static EndpointStatus GetEndpointStatus(EndpointInfo endpoint)
        {
            if (EndpointCollection.TryGetValue(endpoint.GetUrl(), out EndpointStatus status))
            {
                return status;
            }
            return null;
        }

        /// <summary>
        /// Sets endpoint communication to success.
        /// </summary>        
        /// <param name="endpoint">The endpoint</param>
        public static void Success(EndpointInfo endpoint)
        {
            string url = endpoint.GetUrl();
            if (!EndpointCollection.TryGetValue(url, out EndpointStatus status))
            {
                status = new EndpointStatus();
                EndpointCollection.TryAdd(url, status);
            }
            if (PingTimers.ContainsKey(endpoint.Id))
            {
                PingTimers.TryRemove(endpoint.Id, out Timer timer);
                timer?.Dispose();
            }
            status.SetSuccess();
        }

        /// <summary>
        /// Sets endpoint communication failed.
        /// </summary>        
        /// <param name="endpoint">The endpoint</param>
        public static void Error(Settings settings, EndpointInfo endpoint)
        {
            string url = endpoint.GetUrl();
            if (!EndpointCollection.TryGetValue(url, out EndpointStatus status))
            {
                status = new EndpointStatus();
                EndpointCollection.TryAdd(url, status);
            }
            if (!EndpointsInPing.Contains(endpoint.Id) && !Environment.ExecutingContext.IsBackEnd())
            {
                PingEndpoint(settings, endpoint);
            }
            status.SetError();
        }

        /// <summary>
        /// Clears endpoints statuses collection
        /// </summary>
        public static void ClearEndpoints()
        {
            EndpointCollection.Clear();
            EndpointsInPing.Clear();
            foreach (var timer in PingTimers.Values)
            {
                timer?.Dispose();
            }
            PingTimers.Clear();
        }

        /// <summary>
        /// Clears endpoints status
        /// </summary>
        public static void ClearEndpoint(EndpointInfo endpoint)
        {
            if (endpoint != null)
            {
                EndpointCollection.TryRemove(endpoint.GetUrl(), out EndpointStatus status);
                PingTimers.TryRemove(endpoint.Id, out Timer timer);
                timer?.Dispose();
            }
        }

        /// <summary>
        /// Pings endpoind during ping interval
        /// </summary>
        /// <param name="endpointCollection"></param> 
        /// <param name="endpoint"></param>
        private static void PingEndpoint(Settings settings, EndpointInfo endpoint)
        {
            var autoPingInterval = settings?.AutoPingInterval;
            if (autoPingInterval <= 0)
            {
                return;
            }
            if (!EndpointsInPing.Contains(endpoint.Id) && !PingTimers.ContainsKey(endpoint.Id))
            {
                EndpointsInPing.Add(endpoint.Id);
                var statusChecker = new EndpointMonitoringService();
                var timer = new Timer(statusChecker.Ping, settings, 0, autoPingInterval.Value * 1000);
                PingTimers.TryAdd(endpoint.Id, timer);
            }
        }

        private void Ping(object stateInfo)
        {
            Settings settings = (Settings)stateInfo;
            var logger = new Logger(settings);
            ConnectorBase currentConnector = null;
            EndpointInfo endpoint = null;
            if (!string.IsNullOrEmpty(settings.Endpoint))
            {
                currentConnector = new EndpointConnector(settings, logger);
                endpoint = new EndpointInfo(((EndpointConnector)currentConnector).GetEndpoint(settings.Endpoint, logger));
            }
            else if (!string.IsNullOrEmpty(settings.WebServiceURI))
            {
                currentConnector = new WebServiceConnector(settings, logger);
                endpoint = new EndpointInfo(settings.WebServiceURI);
            }
            if (EndpointsInPing.Contains(endpoint.Id))
            {
                EndpointsInPing.RemoveAll(e => e == endpoint.Id);
            }
            currentConnector.IsWebServiceConnectionAvailable();
        }
    }
}
