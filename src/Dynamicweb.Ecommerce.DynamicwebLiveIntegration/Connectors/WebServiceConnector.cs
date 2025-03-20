using Dynamicweb.Core;
using Dynamicweb.DataIntegration.Integration.ERPIntegration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.EndpointMonitoring;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors
{
    /// <summary>
    /// Web service connector
    /// </summary>
    internal class WebServiceConnector(Settings settings, Logger logger, SubmitType submitType, Order order = null) : ConnectorBase(settings, logger, order, submitType)
    {
        private static readonly ILogger LegacyLogger = LogManager.System.GetLogger(LogCategory.DataIntegration, "ERPIntegration");

        /// <summary>
        /// Gets the URL.
        /// </summary>
        /// <value>The URL.</value>
        /// <exception cref="ArgumentException">
        /// Setup does not contain a url for the WebService. Please rerun setup.
        /// or
        /// Cannot find appropriate web service url. Check Web Service Url settings.
        /// </exception>
        private string WebServiceUrl
        {
            get
            {
                string url = UrlHandler.Instance.GetWebServiceUrl(Settings, Order, SubmitType);
                if (!string.IsNullOrEmpty(url))
                {
                    return url;
                }

                if (string.IsNullOrEmpty(Settings.WebServiceURI))
                {
                    throw new ArgumentException("Setup does not contain a url for the WebService. Please rerun setup.");
                }
                else
                {
                    throw new ArgumentException("Cannot find appropriate web service url. Check Web Service Url settings.");
                }
            }
        }

        internal override EndpointInfo GetEndpoint()
        {
            return new EndpointInfo(WebServiceUrl);
        }

        internal static EndpointInfo GetEndpointInfo(string url)
        {
            return new EndpointInfo(url);
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
            return ExecuteConnectionAvailableRequest(new EndpointInfo(WebServiceUrl), out _);
        }

        internal override string Execute(string request)
        {
            return Execute(GetEndpointInfo(UrlHandler.Instance.GetWebServiceUrl(Settings, Order, SubmitType)), request);
        }

        internal override string Execute(EndpointInfo endpoint, string request, TimeSpan responseTimeout)
        {
            string response = ErpServiceCaller.GetDataFromRequestString(endpoint.GetUrl(), Settings.SecurityKey, request, responseTimeout, LegacyLogger);
            EndpointMonitoringService.Success(endpoint);
            return response;
        }

        internal override IEnumerable<string> GetUrls(string multipleUrlsText)
        {
            return UrlHandler.GetWebServiceUrls(multipleUrlsText).Select(u => UrlHandler.GetUrl(u)).Distinct();
        }

        internal override bool IsConnectionAvailableFromBackend(string url, out string error)
        {
            error = null;

            var key = $"IsConnectionAvailableFromBackend{url}";
            var cachedConnectionState = Context.Current?.Items?[key];
            if (cachedConnectionState is not null)
            {
                return Converter.ToBoolean(cachedConnectionState);
            }
            else
            {
                EndpointMonitoringService.ClearEndpoint(GetEndpointInfo(url));

                bool result;
                if (ExecuteConnectionAvailableRequest(GetEndpointInfo(url), out Exception ex))
                {
                    result = true;
                }
                else
                {
                    error = $"Can not connect to the web service: {url} error: {ex?.Message}.";
                    result = false;
                }
                if (Context.Current?.Items is not null)
                {
                    Context.Current.Items[key] = result;
                }
                return result;
            }
        }
    }
}
