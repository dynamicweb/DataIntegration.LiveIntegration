using Dynamicweb.DataIntegration.EndpointManagement;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Environment;
using Dynamicweb.Security.UserManagement;
using Dynamicweb.Security.UserManagement.Common.CustomFields;
using System;
using System.Linq;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration
{
    /// <summary>
    /// UrlHandler to build up connection info for the ERP.
    /// </summary>
    public class UrlHandler
    {
        /// <summary>
        /// The synchronize root
        /// </summary>
        private static readonly object SyncRoot = new object();

        /// <summary>
        /// The instance
        /// </summary>
        private static UrlHandler _instance;

        /// <summary>
        /// Prevents a default instance of the <see cref="UrlHandler"/> class from being created.
        /// </summary>
        private UrlHandler()
        {
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static UrlHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_instance == null)
                        {
                            _instance = new UrlHandler();
                        }
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Gets the URL cache key.
        /// </summary>
        /// <value>The URL cache key.</value>
        private string UrlCacheKey
        {
            get
            {
                return $"{Constants.AddInName}WebServiceURI";
            }
        }

        /// <summary>
        /// Clears the cached URL.
        /// </summary>
        public void ClearCachedUrl()
        {
            if (Context.Current?.Items[UrlCacheKey] != null)
            {
                Context.Current.Items.Remove(UrlCacheKey);
            }
        }

        /// <summary>
        /// Gets the URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns>System.String.</returns>
        internal string GetUrl(string url)
        {
            return (!string.IsNullOrEmpty(url) && url.Contains(";")) ? url.Substring(0, url.IndexOf(";", StringComparison.Ordinal)) : url;
        }

        internal int GetEndpointId(string multipleUrlsText)
        {
            int id = 0;
            if (!string.IsNullOrEmpty(multipleUrlsText))
            {
                string[] urls = GetEndpointUrls(multipleUrlsText);
                if (urls.Length > 0)
                {
                    if (urls.Any(u => u.Contains(";")))
                    {
                        string url;
                        if (ExecutingContext.IsFrontEnd())
                        {
                            url = GetMatchedUrl(urls);
                        }
                        else
                        {
                            url = urls[0];
                        }
                        if (!string.IsNullOrEmpty(url))
                        {
                            id = Core.Converter.ToInt32(GetUrl(url));
                        }
                    }
                    else
                    {
                        id = Core.Converter.ToInt32(GetUrl(urls[0]));
                    }
                }
            }
            return id;
        }

        internal Endpoint GetEndpoint(string multipleUrlsText, bool logIfNotFound, Logger logger)
        {
            if (!string.IsNullOrEmpty(multipleUrlsText))
            {
                int id = GetEndpointId(multipleUrlsText);

                EndpointService endpointService = new EndpointService();
                var endpoint = endpointService.GetEndpointById(id);
                if(logIfNotFound && id > 0 && endpoint == null)
                {
                    logger.Log(ErrorLevel.DebugInfo, $"Endpoint {id} does not exist.");
                }
                return endpoint;
            }
            return null;
        }

        internal Endpoint GetEndpoint(Settings settings, Logger logger)
        {
            Endpoint ret = null;
            string multiLineUrlText = settings.Endpoint;
            if (!string.IsNullOrEmpty(multiLineUrlText))
            {
                if (Context.Current?.Items[UrlCacheKey] != null)
                {
                    ret = (Endpoint)Context.Current.Items[UrlCacheKey];
                }
                else
                {
                    ret = GetEndpoint(multiLineUrlText, false, logger);
                    if (Context.Current?.Items != null)
                    {
                        Context.Current.Items[UrlCacheKey] = ret;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Gets the web service URL for the current context.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetWebServiceUrl(Settings settings)
        {
            string ret = string.Empty;
            if (Context.Current?.Items[UrlCacheKey] != null)
            {
                ret = (string)Context.Current.Items[UrlCacheKey];
            }
            else
            {
                string multiLineUrlText = settings.WebServiceURI;
                string[] urls = GetWebServiceUrls(multiLineUrlText);
                if (urls.Length > 0)
                {
                    if (multiLineUrlText.Contains(";"))
                    {
                        ret = GetMatchedUrl(urls);
                    }
                    else
                    {
                        ret = urls[0];
                    }
                }
                if (Context.Current?.Items != null)
                {
                    Context.Current.Items[UrlCacheKey] = ret;
                }
            }

            return ret;
        }

        private string GetMatchedUrl(string[] urls)
        {
            string ret = null;
            foreach (string url in urls)
            {
                string[] parts = url.Split(new char[] { ';' });
                if (parts.Length > 1)
                {
                    string uri = parts[0];
                    string fieldName = parts[1];
                    string fieldValue = parts[2];
                    if (!string.IsNullOrEmpty(fieldName))
                    {
                        if (fieldName.StartsWith("User."))
                        {
                            if (TreatUserFields(fieldName, fieldValue))
                            {
                                ret = GetUrl(url);
                                break;
                            }
                        }
                        else if (fieldName.StartsWith("Order.") && GetCurrentOrder() != null)
                        {
                            foreach (OrderFieldValue ofv in GetCurrentOrder().OrderFieldValues)
                            {
                                if (ofv != null && ofv.OrderField != null && ofv.OrderField.SystemName == fieldName.Substring(6) && (string)ofv.Value == fieldValue)
                                {
                                    ret = GetUrl(url);
                                    break;
                                }
                            }
                        }
                        else if (fieldName == "Session.Shop" && fieldValue == GetCurrentShopId())
                        {
                            ret = GetUrl(url);
                            break;
                        }
                    }
                }
            }
            return ret;
        }        

        /// <summary>
        /// Gets the current order.
        /// </summary>
        /// <returns>Order.</returns>
        private Order GetCurrentOrder()
        {
            Order ret = Common.Context.Cart;

            if (ret == null && !string.IsNullOrEmpty(Context.Current?.Request["orderid"]))
            {
                ret = Services.Orders.GetById(Context.Current.Request.GetString("orderid"));
            }

            return ret;
        }

        /// <summary>
        /// Gets the current shop identifier.
        /// </summary>
        /// <returns>System.String.</returns>
        private string GetCurrentShopId()
        {
            string ret = string.Empty;

            if (Dynamicweb.Frontend.PageView.Current() != null && Dynamicweb.Frontend.PageView.Current().Area != null)
            {
                ret = Dynamicweb.Frontend.PageView.Current().Area.EcomShopId;
            }
            else if (!string.IsNullOrEmpty(Context.Current?.Request["shopid"]))
            {
                ret = Context.Current.Request["shopid"];
            }
            else if (!string.IsNullOrEmpty((string)Context.Current?.Items["shopid"]))
            {
                ret = (string)Context.Current.Items["shopid"];
            }

            return ret;
        }

        /// <summary>
        /// Get webservices urls count
        /// </summary>
        /// <returns></returns>
        public int GetWebServiceUrlsCount(Settings settings)
        {
            string multilineUrlText = settings.WebServiceURI;
            if (!string.IsNullOrEmpty(multilineUrlText))
            {
                if (multilineUrlText.Contains("\r\n") || multilineUrlText.Contains("\n"))
                {
                    return multilineUrlText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Length;
                }
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Gets the web service urls.
        /// </summary>
        /// <param name="multilineUrlText">The multiline URL text.</param>
        /// <returns>System.String[].</returns>
        internal string[] GetWebServiceUrls(string multilineUrlText)
        {
            string[] ret;

            if (!string.IsNullOrEmpty(multilineUrlText))
            {
                ret = new string[] { multilineUrlText };
                if (multilineUrlText.Contains("\r\n") || multilineUrlText.Contains("\n"))
                {
                    ret = multilineUrlText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                }
            }
            else
            {
                ret = new string[] { };
            }

            return ret;
        }

        /// <summary>
        /// Gets the endpoint urls.
        /// </summary>
        /// <param name="multipleUrlsText">The multiple Urls text.</param>
        /// <returns>System.String[].</returns>
        internal string[] GetEndpointUrls(string multipleUrlsText)
        {
            string[] ret;

            if (!string.IsNullOrEmpty(multipleUrlsText))
            {
                ret = new string[] { multipleUrlsText };
                if (multipleUrlsText.Contains(","))
                {
                    ret = multipleUrlsText.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                }
            }
            else
            {
                ret = new string[] { };
            }

            return ret;
        }

        /// <summary>
        /// Treats the user fields.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="fieldValue">The field value.</param>
        /// <returns><c>true</c> if the user field matched the passed <c>fieldName</c>, <c>false</c> otherwise.</returns>
        private bool TreatUserFields(string fieldName, string fieldValue)
        {
            User user = Helpers.GetCurrentExtranetUser();

            if (user == null)
            {
                return false;
            }

            if ((fieldName == "User.Company" && user.Company == fieldValue) || (fieldName == "User.Department" && user.Department == fieldValue))
            {
                return true;
            }
            else
            {
                foreach (CustomFieldValue cfv in user.CustomFieldValues)
                {
                    // fieldName.Substring: Remove the "User." prefix to
                    // obtain system name only
                    if (cfv != null
                        && cfv.CustomField != null
                        && cfv.CustomField.SystemName == fieldName.Substring(5)
                        && (string)cfv.Value == fieldValue)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}