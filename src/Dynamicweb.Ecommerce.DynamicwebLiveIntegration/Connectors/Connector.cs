using Dynamicweb.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.EndpointMonitoring;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Licensing;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Extensibility.Notifications;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Xml;
using static Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Notifications.Communication;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors
{
    /// <summary>
    /// Main class to interact with a remote ERP.
    /// </summary>
    internal static class Connector
    {
        /// <summary>
        /// The maximum retry count
        /// </summary>
        private static readonly int MaxRetryCount = 3;

        /// <summary>
        /// The retry interval
        /// </summary>
        private static readonly int RetryInterval = 500;

        private static ConcurrentDictionary<string, DateTime> InstanceIdTooManyRequestsCollection = new ConcurrentDictionary<string, DateTime>();
        private static int TooManyRequestsTimeoutSeconds = 60;

        public static bool EnableThrowExceptions
        {
            get
            {
                return Core.Converter.ToBoolean(Context.Current?.Items?["DynamicwebLiveIntegrationAddInThrowExceptions"]);
            }
            set
            {
                if (Context.Current?.Items != null)
                {
                    Context.Current.Items["DynamicwebLiveIntegrationAddInThrowExceptions"] = value;
                }
            }
        }

        static Connector()
        {
            int timeout = SystemConfiguration.Instance.GetInt32($"{Constants.LiveIntegrationSettingsKey}/TooManyRequestsTimeoutSeconds");
            if (timeout > 0)
            {
                TooManyRequestsTimeoutSeconds = timeout;
            }
        }

        private static ConnectorBase GetConnector(Settings settings, Logger logger, Order order = null)
        {
            if (settings == null || string.IsNullOrEmpty(settings.Endpoint))
            {
                return new WebServiceConnector(settings, logger, order);
            }
            else
            {
                return new EndpointConnector(settings, logger, order);
            }
        }

        /// <summary>
        /// Calculates the order.
        /// </summary>
        /// <param name="orderXml">The order XML.</param>
        /// <param name="orderId">The order identifier.</param>
        /// <param name="createOrder">if set to <c>true</c> [create order].</param>
        /// <param name="error">The error.</param>
        /// <returns>XmlDocument.</returns>
        [Obsolete("Use CalculateOrder(Settings settings, string orderXml, Order order, bool createOrder, out Exception error, Logger logger) instead")]
        public static XmlDocument CalculateOrder(Settings settings, string orderXml, string orderId, bool createOrder, out Exception error, Logger logger)
        {
            return CalculateOrder(settings, orderXml, Services.Orders.GetById(orderId), createOrder, out error, logger);
        }

        /// <summary>
        /// Calculates the order.
        /// </summary>
        /// <param name="orderXml">The order XML.</param>
        /// <param name="order">The order</param>
        /// <param name="createOrder">if set to <c>true</c> [create order].</param>
        /// <param name="error">The error.</param>
        /// <returns>XmlDocument.</returns>
        public static XmlDocument CalculateOrder(Settings settings, string orderXml, Order order, bool createOrder, out Exception error, Logger logger)
        {
            error = null;
            XmlDocument document = null;

            try
            {
                Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.Connector.CalculateOrder START");
                // only retry if is not create order, for create order schedule task will send later
                document = Communicate(settings, orderXml, $"CalculateOrder (ID: {order.Id}, CreateOrder: {createOrder})", logger, !createOrder, true, order);
                Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.Connector.CalculateOrder END");
            }
            catch (Exception ex)
            {
                logger.Log(ErrorLevel.ConnectionError,
                    $"Error CalculateOrder Order Id:'{order.Id}' CreateOrder:'{createOrder}' Message:'{ex.Message}'.");
                error = ex;
            }

            if (EnableThrowExceptions && error != null)
            {
                throw error;
            }

            return document;
        }

        /// <summary>
        /// Clears the cache.
        /// </summary>
        public static void ClearCache()
        {
            EndpointMonitoringService.ClearEndpoints();
        }

        /// <summary>
        /// Gets the products information.
        /// </summary>
        /// <param name="productsXml">The products XML.</param>
        /// <param name="retry">Retry request on error.</param>
        /// <returns>XmlDocument.</returns>
        public static XmlDocument GetProductsInfo(Settings settings, string productsXml, Logger logger, out HttpStatusCode httpStatusCode, bool retry = false)
        {
            Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.Connector.GetProductsInfo START");
            var result = Communicate(settings, productsXml, "GetProductsInfo", logger, out httpStatusCode, retry); ;
            Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.Connector.GetProductsInfo END");
            return result;
        }

        /// <summary>
        /// Determines whether the web service connection is available.
        /// </summary>
        public static bool IsWebServiceConnectionAvailable(Settings settings)
        {
            return IsWebServiceConnectionAvailable(settings, new Logger(settings));
        }

        /// <summary>
        /// Determines whether the web service connection is available.
        /// </summary>
        public static bool IsWebServiceConnectionAvailable(Settings settings, Logger logger)
        {
            if (settings is null)
            {
                return true;
            }
            else
            {
                var connector = GetConnector(settings, logger);
                return connector.IsWebServiceConnectionAvailable();
            }
        }

        /// <summary>
        /// Retrieves data from the request string.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>XmlDocument.</returns>
        public static XmlDocument RetrieveDataFromRequestString(Settings settings, string request, Logger logger)
        {
            Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.Connector.RetrieveDataFromRequestString START");
            var result = Communicate(settings, request, "RetrieveDataFromRequestString", logger);
            Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.Connector.RetrieveDataFromRequestString END");
            return result;
        }

        /// <summary>
        /// Communicates the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="referenceName">Name of the reference.</param>
        /// <param name="retry">if set to <c>true</c> [retry].</param>
        /// <param name="throwException">if set to <c>true</c> [throw exception].</param>
        /// <returns>XmlDocument.</returns>
        private static XmlDocument Communicate(Settings settings, string request, string referenceName, Logger logger, bool retry = true, bool throwException = false, Order order = null)
        {
            return Communicate(settings, request, referenceName, logger, out _, retry, throwException, order);
        }

        /// <summary>
        /// Communicates the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="referenceName">Name of the reference.</param>
        /// <param name="retry">if set to <c>true</c> [retry].</param>
        /// <param name="throwException">if set to <c>true</c> [throw exception].</param>
        /// <returns>XmlDocument.</returns>
        private static XmlDocument Communicate(Settings settings, string request, string referenceName, Logger logger, out HttpStatusCode httpStatusCode, bool retry = true, bool throwException = false, Order order = null)
        {
            XmlDocument result = null;
            int retryCount = 0;
            httpStatusCode = HttpStatusCode.OK;
            var connector = GetConnector(settings, logger, order);

            if (InstanceIdTooManyRequestsCollection.TryGetValue(settings.InstanceId, out DateTime lastErrorTime) &&
                DateTime.Now.Subtract(lastErrorTime).TotalSeconds < TooManyRequestsTimeoutSeconds)
            {
                logger.Log(ErrorLevel.DebugInfo, $"Skip due to 429 error timeout: {referenceName} Request: '{request}'.");
                return null;
            }

            // if ERP is down and in retry mode wait!
            while (!connector.IsWebServiceConnectionAvailable())
            {
                if (!retry || retryCount >= MaxRetryCount)
                {
                    return null;
                }

                ++retryCount;
                System.Threading.Thread.Sleep(RetryInterval);
            }

            var endpoint = connector.GetEndpoint();
            string url = endpoint?.GetUrl();

            if (!LicenseService.IsLicenseValid(url))
            {
                logger.Log(ErrorLevel.DebugInfo, $"Request send failed. License is not valid.");
                Diagnostics.ExecutionTable.Current.Add($"DynamicwebLiveIntegration: Request send failed. License is not valid.");
                return null;
            }

            Exception exception;
            do
            {
                exception = null;
                string erpXmlResponse = null;
                try
                {
                    NotificationManager.Notify(OnBeforeErpCommunication, new OnBeforeErpCommunicationArgs(request, referenceName, settings, logger));

                    logger.Log(ErrorLevel.DebugInfo, $"Request {referenceName} sent: '{request}'.");
                    Diagnostics.ExecutionTable.Current.Add($"DynamicwebLiveIntegration: Request {referenceName} sent: '{WebUtility.HtmlEncode(request)}'.");

                    erpXmlResponse = connector.Execute(endpoint, request);

                    if (!string.IsNullOrEmpty(erpXmlResponse))
                    {
                        logger.Log(ErrorLevel.DebugInfo, $"Response {referenceName} received: '{erpXmlResponse}'.");
                        Diagnostics.ExecutionTable.Current.Add($"DynamicwebLiveIntegration: Response {referenceName} received: '{WebUtility.HtmlEncode(erpXmlResponse)}'.");

                        if (!Helpers.ParseResponseToXml(erpXmlResponse, logger, out result))
                        {
                            result = null;
                            exception = new Exception("Response is not valid XML");
                        }
                        if (result != null && !LicenseService.ValidateLicense(url, result, logger))
                        {
                            logger.Log(ErrorLevel.DebugInfo, $"Response failed. License is not valid.");
                            Diagnostics.ExecutionTable.Current.Add($"DynamicwebLiveIntegration: Response failed. License is not valid.");
                            result = null;
                            retry = false;
                        }
                    }
                    else
                    {
                        logger.Log(ErrorLevel.ResponseError, $"Response {referenceName} returned null.");
                        Diagnostics.ExecutionTable.Current.Add($"DynamicwebLiveIntegration: Response {referenceName} returned null.");
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;

                    logger.Log(ErrorLevel.ConnectionError, $"An error occurred while calling {referenceName} from Web Service: '{ex.Message}'.");
                    Diagnostics.ExecutionTable.Current.Add($"DynamicwebLiveIntegration: An error occurred while calling {referenceName} from Web Service: '{ex.Message}'.");

                    bool skipError = false;
                    //Do not ping Endpoints with Internal Server Error 500 (that means that the request was received but generating response failed)
                    if (ex?.InnerException != null && ex.InnerException is WebException webException)
                    {
                        if (webException != null && webException.Response != null && webException.Response is HttpWebResponse httpWebResponse)
                        {
                            httpStatusCode = httpWebResponse.StatusCode;
                            if (httpWebResponse != null && httpWebResponse.StatusCode == HttpStatusCode.InternalServerError)
                            {
                                skipError = true;
                                retry = false;
                            }
                        }
                    }
                    //ConnectionError: An error occurred while calling GetProductsInfo from Web Service: 'The remote server returned an error: (429).'
                    if (!skipError && !string.IsNullOrEmpty(ex.Message) && ex.Message.Contains("(429)"))
                    {
                        skipError = true;
                        retry = false;
                        InstanceIdTooManyRequestsCollection.TryAdd(settings.InstanceId, DateTime.Now);
                    }
                    if (!skipError)
                    {
                        connector.Error(endpoint);
                    }

                    NotificationManager.Notify(OnAfterErpException, new OnAfterErpExceptionArgs(request, erpXmlResponse, referenceName, ex, settings, logger));
                }
                finally
                {
                    NotificationManager.Notify(OnAfterErpCommunication, new OnAfterErpCommunicationArgs(request, erpXmlResponse, result, referenceName, exception, settings, logger));
                }

                // no retry or xml reply from ERP then leave
                if (!retry || result != null)
                {
                    break;
                }

                // in retry mode, wait some time
                ++retryCount;
                System.Threading.Thread.Sleep(RetryInterval);
            }
            while (retryCount < MaxRetryCount);

            if ((throwException || EnableThrowExceptions) && exception != null)
            {
                throw exception;
            }
            return result;
        }

        internal static string RetrievePDF(Settings settings, string requestString)
        {
            Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.Connector.RetrievePDF START");
            string base64EncodedPDF;
            var logger = new Logger(settings);
            try
            {
                logger.Log(ErrorLevel.DebugInfo, string.Format("Request RetrievePDF sent: '{0}'.", requestString));
                base64EncodedPDF = GetConnector(settings, logger).Execute(requestString);
                logger.Log(ErrorLevel.DebugInfo, string.Format("Response RetrievePDF received: '{0}'.", base64EncodedPDF));
            }
            catch (Exception ex)
            {
                logger.Log(ErrorLevel.ResponseError, string.Format("Response RetrievePDF returned error: '{0}'.", ex.Message));
                throw;
            }
            Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.Connector.RetrievePDF END");
            return base64EncodedPDF;
        }
    }
}