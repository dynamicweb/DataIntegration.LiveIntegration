using Dynamicweb.Core;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration
{
    /// <summary>
    /// Global class with global settings.
    /// </summary>
    internal static class Global
    {
        /// <summary>
        /// Gets a value that determines if the integration is active.
        /// </summary>
        /// <value>Returns<c>true</c> when live integration is enabled and the executing context is not the backend, <c>false</c> otherwise.</value>
        public static bool IsIntegrationActive(Settings settings)
        {
            return settings != null ? settings.IsLiveIntegrationEnabled : false;
        }

        /// <summary>
        /// Gets a value that determines if product information should be lazy loaded.
        /// </summary>
        /// <value>Returns <c>true</c> if lazy loaded is enabled in the settings and the current request is not a lazy loaded request, <c>false</c> otherwise.</value>
        public static bool IsProductLazyLoad(Settings settings)
        {
            return settings.LazyLoadProductInfo && !Converter.ToBoolean(Context.Current.Request["getproductinfo"]);
        }

        /// <summary>
        /// Gets a value that determines whether cart communication is enabled.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="orderComplete">Indicates if the current order is completed.</param>
        /// <returns>Returns <c>true</c> when the cart communication type is full or when it's "only on order complete" and the current order is complete, <c>false</c> otherwise.</returns>
        public static bool EnableCartCommunication(Settings settings, bool orderComplete)
        {
            string enableCartCommunication = settings.CartCommunicationType;

            switch (enableCartCommunication)
            {
                case Constants.CartCommunicationType.None:
                    return false;

                case Constants.CartCommunicationType.Full:
                    return true;

                case Constants.CartCommunicationType.OnlyOnOrderComplete:
                    return orderComplete;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Current web site ShopId
        /// </summary>
        public static string CurrentShopId
        {
            get
            {
                var cacheValue = Context.Current?.Items?["DynamicwebLiveIntegrationCurrentShopId"];
                if (cacheValue != null)
                {
                    return (string)cacheValue;
                }
                else
                {
                    string result;
                    var pageView = Dynamicweb.Frontend.PageView.Current();
                    if (pageView?.Area != null)
                    {
                        result = pageView.Area.EcomShopId;
                    }
                    else
                    {
                        result = Context.Current?.Request?["shopid"];
                    }
                    if (Context.Current?.Items != null)
                    {
                        Context.Current.Items["DynamicwebLiveIntegrationCurrentShopId"] = result;
                    }
                    return result;
                }
            }
        }
    }
}