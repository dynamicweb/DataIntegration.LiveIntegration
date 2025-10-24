using Dynamicweb.Core;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions;
using Dynamicweb.Frontend;

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
            var active = settings != null ? settings.IsLiveIntegrationEnabled : false;
            if(active && settings.AreaId > 0)
            {
                var pageView = PageView.Current();
                int currentAreaId = (pageView is object) ? pageView.Area?.ID ?? 0 : Converter.ToInt32(Context.Current?.Request?["AreaId"]);
                active = currentAreaId > 0 ? currentAreaId == settings.AreaId : active;
            }                
            return active;
        }

        /// <summary>
        /// Gets a value that determines if product information should be lazy loaded.
        /// </summary>
        /// <value>Returns <c>true</c> if lazy loaded is enabled in the settings and the current request is not a lazy loaded request, <c>false</c> otherwise.</value>
        public static bool IsProductLazyLoad(Settings settings)
        {
            return settings.LazyLoadProductInfo && !Converter.ToBoolean(Context.Current?.Request?["getproductinfo"]);
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

            return enableCartCommunication switch
            {
                Constants.CartCommunicationType.None => false,
                Constants.CartCommunicationType.Full => true,
                Constants.CartCommunicationType.OnlyOnOrderComplete => orderComplete,
                Constants.CartCommunicationType.CartOnly => !orderComplete,
                _ => false,
            };
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

        public static string GetShopId(PageView pageView)
        {
            string result;
            if (pageView?.Area != null)
            {
                result = pageView.Area.EcomShopId;
            }
            else
            {
                result = CurrentShopId;
            }
            return result;
        }

        public static bool IsLazyLoadingForProductInfoEnabled(Settings settings)
        {
            return Global.IsIntegrationActive(settings) && settings.EnableLivePrices && Connector.IsWebServiceConnectionAvailable(settings, SubmitType.Live)
                       && (settings.LiveProductInfoForAnonymousUsers || Helpers.GetCurrentExtranetUser() != null)
                       && (Helpers.GetCurrentExtranetUser() == null || !Helpers.GetCurrentExtranetUser().IsLiveIntegrationPricesDisabled())
                       && settings.LazyLoadProductInfo;
        }
    }
}