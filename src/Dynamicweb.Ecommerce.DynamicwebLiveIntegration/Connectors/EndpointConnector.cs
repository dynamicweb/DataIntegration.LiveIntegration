﻿using Dynamicweb.DataIntegration.EndpointManagement;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.EndpointMonitoring;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using System;
using System.Collections.Generic;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors
{
    /// <summary>
    /// Endpoint Connector
    /// </summary>
    internal class EndpointConnector : ConnectorBase
    {        
        public EndpointConnector(Settings settings, Logger logger) : base(settings, logger)
        {
        }

        internal override EndpointInfo GetEndpoint()
        {
            return new EndpointInfo(GetEndpoint(Settings.Endpoint, Logger));
        }        

        internal Endpoint GetEndpoint(string multipleUrlsText, Logger logger)
        {
            if (!string.IsNullOrEmpty(multipleUrlsText))
            {
                var endpoint = UrlHandler.Instance.GetEndpoint(multipleUrlsText, false, logger);
                if (endpoint == null)
                {
                    throw new ArgumentException("Cannot find appropriate endpoint. Check Endpoint settings.");
                }
                return endpoint;
            }
            else
            {
                return null;
            }
        }

        internal EndpointInfo GetEndpointInfo(Endpoint endpoint)
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
            return Execute(GetEndpointInfo(UrlHandler.Instance.GetEndpoint(Settings, Logger)), request);
        }

        internal override IEnumerable<string> GetUrls(string multipleUrlsText)
        {
            return UrlHandler.Instance.GetEndpointUrls(multipleUrlsText);
        }

        internal override bool IsConnectionAvailableFromBackend(string endpointId, out string error)
        {
            error = null;
            var endpoint = UrlHandler.Instance.GetEndpoint(endpointId, false, Logger);
            if (endpoint != null)
            {
                EndpointMonitoringService.ClearEndpoint(GetEndpointInfo(endpoint));

                if (ExecuteConnectionAvailableRequest(endpoint, out Exception ex))
                {
                    return true;
                }
                else
                {
                    error = $"Can not connect to the endpoint: {endpoint.Name} error: {ex?.Message}.";
                    return false;
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
