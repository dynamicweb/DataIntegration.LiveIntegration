using Dynamicweb.Core;
using Dynamicweb.DataIntegration.EndpointManagement;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.EndpointMonitoring;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.Orders;
using System;
using System.Collections.Generic;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors
{
    /// <summary>
    /// Endpoint Connector
    /// </summary>
    internal class EndpointConnector(Settings settings, Logger logger, SubmitType submitType, Order order = null) : ConnectorBase(settings, logger, order, submitType)
    {
        internal override EndpointInfo GetEndpoint()
        {
            return new EndpointInfo(GetEndpoint(Settings.Endpoint, Logger));
        }

        internal Endpoint GetEndpoint(string multipleUrlsText, Logger logger)
        {
            if (!string.IsNullOrEmpty(multipleUrlsText))
            {
                var endpoint = UrlHandler.Instance.GetEndpoint(multipleUrlsText, false, logger, Order, SubmitType);
                return endpoint ?? throw new ArgumentException("Cannot find appropriate endpoint. Check Endpoint settings.");
            }
            else
            {
                return null;
            }
        }

        internal static EndpointInfo GetEndpointInfo(Endpoint endpoint)
        {
            return new EndpointInfo(endpoint);
        }

        /// <summary>
        /// Determines whether the web service connection is available.
        /// </summary>
        internal override bool IsWebServiceConnectionAvailable()
        {
            if (Settings == null || !Settings.IsLiveIntegrationEnabled)
            {
                return false;
            }
            return ExecuteConnectionAvailableRequest(new EndpointInfo(GetEndpoint(Settings.Endpoint, Logger)), out _);
        }

        internal bool ExecuteConnectionAvailableRequest(Endpoint endpoint, out Exception error)
        {
            return ExecuteConnectionAvailableRequest(GetEndpointInfo(endpoint), out error);
        }

        internal override string Execute(EndpointInfo endpoint, string request, TimeSpan responseTimeout)
        {
            string response = null;
            if (endpoint?.Endpoint != null)
            {
                EndpointService endpointService = new EndpointService();
                response = endpointService.Execute(endpoint.Endpoint.Id, endpointService.GetDynamicwebServiceSoapBody(endpoint.Endpoint, request), responseTimeout);
                EndpointMonitoringService.Success(endpoint);
            }
            return response;
        }

        internal override string Execute(string request)
        {
            return Execute(GetEndpointInfo(UrlHandler.Instance.GetEndpoint(Settings, Logger, Order, SubmitType)), request);
        }

        internal override IEnumerable<string> GetUrls(string multipleUrlsText)
        {
            return UrlHandler.GetEndpointUrls(multipleUrlsText);
        }

        internal override bool IsConnectionAvailableFromBackend(string endpointId, out string error)
        {
            error = null;
            var endpoint = UrlHandler.Instance.GetEndpoint(endpointId, false, Logger, Order, SubmitType);
            if (endpoint != null)
            {
                var key = $"IsConnectionAvailableFromBackend{endpointId}";
                var cachedConnectionState = Context.Current?.Items?[key];
                if (cachedConnectionState is not null)
                {
                    return Converter.ToBoolean(cachedConnectionState);
                }
                else
                {
                    EndpointMonitoringService.ClearEndpoint(GetEndpointInfo(endpoint));

                    bool result;
                    if (ExecuteConnectionAvailableRequest(endpoint, out Exception ex))
                    {
                        result = true;
                    }
                    else
                    {
                        error = $"Can not connect to the endpoint: {endpoint.Name} error: {ex?.Message}.";
                        result = false;
                    }
                    if (Context.Current?.Items is not null)
                    {
                        Context.Current.Items[key] = result;
                    }
                    return result;
                }
            }
            else
            {
                error = $"Endpoint with Id:{endpointId} can not be found.";
                return false;
            }
        }
    }
}
