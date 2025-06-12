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
        private static readonly UrlHandler _instance = new UrlHandler();

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static UrlHandler()
        {
        }

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
            get => _instance;            
        }

        /// <summary>
        /// Gets the URL cache key.
        /// </summary>
        /// <value>The URL cache key.</value>
        private static string UrlCacheKey
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
        internal static string GetUrl(string url)
        {
            return (!string.IsNullOrEmpty(url) && url.Contains(';')) ? url[..url.IndexOf(';', StringComparison.Ordinal)] : url;
        }

        internal int GetEndpointId(string multipleUrlsText, Order order, SubmitType submitType)
        {
            int id = 0;
            if (!string.IsNullOrEmpty(multipleUrlsText))
            {
                string[] urls = GetEndpointUrls(multipleUrlsText);
                if (urls.Length > 0)
                {
                    if (urls.Any(u => u.Contains(';')))
                    {
                        string url;
                        bool isFrontEnd = ExecutingContext.IsFrontEnd();
                        if (isFrontEnd || order != null)
                        {
                            url = GetMatchedUrl(urls, order, submitType);

                            if(!isFrontEnd && string.IsNullOrEmpty(url))
                            {
                                url = urls[0];
                            }
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

        internal Endpoint GetEndpoint(string multipleUrlsText, bool logIfNotFound, Logger logger, Order order, SubmitType submitType)
        {
            if (!string.IsNullOrEmpty(multipleUrlsText))
            {
                int id = GetEndpointId(multipleUrlsText, order, submitType);

                EndpointService endpointService = new EndpointService();
                var endpoint = endpointService.GetEndpointById(id);
                if (logIfNotFound && id > 0 && endpoint == null)
                {
                    logger.Log(ErrorLevel.DebugInfo, $"Endpoint {id} does not exist.");
                }
                return endpoint;
            }
            return null;
        }

        internal Endpoint GetEndpoint(Settings settings, Logger logger, Order order, SubmitType submitType)
        {
            Endpoint ret = null;
            string multiLineUrlText = settings.Endpoint;
            if (!string.IsNullOrEmpty(multiLineUrlText))
            {
                if (Context.Current?.Items[UrlCacheKey] != null && ExecutingContext.IsFrontEnd())
                {
                    ret = (Endpoint)Context.Current.Items[UrlCacheKey];
                }
                else
                {
                    ret = GetEndpoint(multiLineUrlText, false, logger, order, submitType);
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
        [Obsolete("Use GetWebServiceUrl(Settings settings, Order order) instead")]
        public string GetWebServiceUrl(Settings settings) => GetWebServiceUrl(settings, null, SubmitType.Live);

        /// <summary>
        /// Gets the web service URL for the current context.
        /// </summary>
        /// <returns>System.String.</returns>
        [Obsolete("Use GetWebServiceUrl(Settings settings, Order order, SubmitType submitType) instead")]
        public string GetWebServiceUrl(Settings settings, Order order) => GetWebServiceUrl(settings, order, SubmitType.Live);


        /// <summary>
        /// Gets the web service URL for the current context.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetWebServiceUrl(Settings settings, Order order, SubmitType submitType)
        {
            string ret = string.Empty;
            if (Context.Current?.Items[UrlCacheKey] != null && ExecutingContext.IsFrontEnd())
            {
                ret = (string)Context.Current.Items[UrlCacheKey];
            }
            else
            {
                string multiLineUrlText = settings.WebServiceURI;
                string[] urls = GetWebServiceUrls(multiLineUrlText);
                if (urls.Length > 0)
                {
                    if (multiLineUrlText.Contains(';'))
                    {
                        ret = GetMatchedUrl(urls, order, submitType);
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

        private string GetMatchedUrl(string[] urls, Order order, SubmitType submitType)
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
                            if (TreatUserFields(fieldName, fieldValue, order, submitType))
                            {
                                ret = GetUrl(url);
                                break;
                            }
                        }
                        else if (fieldName.StartsWith("Order.") && GetCurrentOrder(order, submitType) != null)
                        {
                            foreach (OrderFieldValue ofv in GetCurrentOrder(order, submitType).OrderFieldValues)
                            {
                                if (ofv != null && ofv.OrderField != null && ofv.OrderField.SystemName == fieldName[6..] && (string)ofv.Value == fieldValue)
                                {
                                    ret = GetUrl(url);
                                    break;
                                }
                            }
                        }
                        else if (fieldName == "Session.Shop" && fieldValue == GetCurrentShopId(order, submitType))
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
        private static Order GetCurrentOrder(Order order, SubmitType submitType)
        {
            if(LiveContext.IsBackEnd(submitType) && order != null)
            {
                return order;
            }

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
        private static string GetCurrentShopId(Order order, SubmitType submitType)
        {
            if (LiveContext.IsBackEnd(submitType) && order != null)
            {
                return order.ShopId;
            }

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
                if (multilineUrlText.Contains("\r\n") || multilineUrlText.Contains('\n'))
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
        internal static string[] GetWebServiceUrls(string multilineUrlText)
        {
            string[] ret;

            if (!string.IsNullOrEmpty(multilineUrlText))
            {
                ret = new string[] { multilineUrlText };
                if (multilineUrlText.Contains("\r\n") || multilineUrlText.Contains('\n'))
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
        internal static string[] GetEndpointUrls(string multipleUrlsText)
        {
            string[] ret;

            if (!string.IsNullOrEmpty(multipleUrlsText))
            {
                ret = new string[] { multipleUrlsText };
                if (multipleUrlsText.Contains(','))
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
        private static bool TreatUserFields(string fieldName, string fieldValue, Order order, SubmitType submitType)
        {            
            User user = (LiveContext.IsBackEnd(submitType) && order != null) ? UserManagementServices.Users.GetUserById(order.CustomerAccessUserId) :
                Helpers.GetCurrentExtranetUser();

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