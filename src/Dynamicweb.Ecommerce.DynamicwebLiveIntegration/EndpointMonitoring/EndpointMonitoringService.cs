using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.EndpointMonitoring
{
    /// <summary>
    /// EndpointMonitoringService
    /// </summary>
    internal class EndpointMonitoringService
    {
        private static readonly ConcurrentDictionary<string, EndpointStatus> EndpointCollection = new ConcurrentDictionary<string, EndpointStatus>();
        private static readonly ConcurrentDictionary<string, string> EndpointsInPing = new ConcurrentDictionary<string, string>();
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

            if (EndpointsInPing.ContainsKey(endpoint.Id))
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
            EndpointsInPing.TryRemove(endpoint.Id, out _);
            status.SetSuccess();
        }

        /// <summary>
        /// Sets endpoint communication failed.
        /// </summary>        
        /// <param name="endpoint">The endpoint</param>
        public static void Error(Settings settings, EndpointInfo endpoint, SubmitType submitType)
        {
            string url = endpoint.GetUrl();
            if (!EndpointCollection.TryGetValue(url, out EndpointStatus status))
            {
                status = new EndpointStatus();
                EndpointCollection.TryAdd(url, status);
            }
            if (!EndpointsInPing.ContainsKey(endpoint.Id) && !LiveContext.IsBackEnd(submitType))
            {
                PingEndpoint(settings, endpoint, submitType);
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
                EndpointCollection.TryRemove(endpoint.GetUrl(), out _);
                PingTimers.TryRemove(endpoint.Id, out Timer timer);
                timer?.Dispose();
            }
        }

        /// <summary>
        /// Pings endpoind during ping interval
        /// </summary>
        /// <param name="endpointCollection"></param> 
        /// <param name="endpoint"></param>
        private static void PingEndpoint(Settings settings, EndpointInfo endpoint, SubmitType submitType)
        {
            var autoPingInterval = settings?.AutoPingInterval;
            if (autoPingInterval <= 0)
            {
                return;
            }
            if (!EndpointsInPing.ContainsKey(endpoint.Id) && !PingTimers.ContainsKey(endpoint.Id))
            {
                EndpointsInPing.TryAdd(endpoint.Id, null);
                var statusChecker = new EndpointMonitoringService();
                var timer = new Timer(statusChecker.Ping, new Tuple<Settings, SubmitType>(settings, submitType), 0, autoPingInterval.Value * 1000);
                PingTimers.TryAdd(endpoint.Id, timer);
            }
        }

        private static (Settings Settings, SubmitType SubmitType) GetStateInfo(object stateInfo)
        {
            var state = (Tuple<Settings, SubmitType>)stateInfo;
            return (state.Item1, state.Item2);
        }

        private void Ping(object stateInfo)
        {
            var state = GetStateInfo(stateInfo);
            Settings settings = state.Settings;
            SubmitType submitType = state.SubmitType;

            var logger = new Logger(settings);
            ConnectorBase currentConnector = null;
            EndpointInfo endpoint = null;
            if (!string.IsNullOrEmpty(settings.Endpoint))
            {
                currentConnector = new EndpointConnector(settings, logger, submitType);
                endpoint = new EndpointInfo(((EndpointConnector)currentConnector).GetEndpoint(settings.Endpoint, logger));
            }
            else if (!string.IsNullOrEmpty(settings.WebServiceURI))
            {
                currentConnector = new WebServiceConnector(settings, logger, submitType);
                endpoint = new EndpointInfo(settings.WebServiceURI);
            }
            EndpointsInPing.TryRemove(endpoint.Id, out _);
            currentConnector.IsWebServiceConnectionAvailable();
        }
    }
}
