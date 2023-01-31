using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
using Dynamicweb.Extensibility.Notifications;
using Dynamicweb.Frontend;
using System;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.NotificationSubscribers
{
    /// <summary>
    /// Adds template tags for failed order communication, if any.
    /// </summary>
    /// <seealso cref="NotificationSubscriberBase" />
    [Subscribe(Dynamicweb.Notifications.Standard.Page.Loaded)]
    public class PageLoaded : NotificationSubscriberBase
    {
        /// <summary>
        /// Handles the notification.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="args">The args.</param>
        public override void OnNotify(string notification, NotificationArgs args)
        {
            if (Context.Current != null)
            {
                bool isWebServiceConnectionAvailable = false;
                bool isLazyLoadingForProductInfoEnabled = false;

                Dynamicweb.Notifications.Standard.Page.LoadedArgs loadedArgs = args as Dynamicweb.Notifications.Standard.Page.LoadedArgs;
                if (loadedArgs != null)
                {                 
                    var settings = SettingsManager.GetSettingsByShop(Global.GetShopId(loadedArgs.PageViewInstance));
                    if (settings != null && Global.IsIntegrationActive(settings))
                    {
                        isWebServiceConnectionAvailable = Connector.IsWebServiceConnectionAvailable(settings);
                        isLazyLoadingForProductInfoEnabled = Global.IsLazyLoadingForProductInfoEnabled(settings);

                        if (Context.Current.Session != null && Convert.ToBoolean(Context.Current.Session["DynamicwebLiveIntegration.OrderExportFailed"]))
                        {
                            if (loadedArgs?.PageViewInstance?.Template != null)
                            {
                                loadedArgs.PageViewInstance.Template.SetTag("LiveIntegration.OrderExportFailed", true);
                                loadedArgs.PageViewInstance.Template.SetTag("LiveIntegration.FailedOrderID", Convert.ToString(Context.Current.Session["DynamicwebLiveIntegration.FailedOrderId"]));
                            }
                        }
                    }
                }
                Context.Current.Items["IsWebServiceConnectionAvailable"] = isWebServiceConnectionAvailable;
                Context.Current.Items["IsLazyLoadingForProductInfoEnabled"] = isLazyLoadingForProductInfoEnabled;
            }
        }
    }
}