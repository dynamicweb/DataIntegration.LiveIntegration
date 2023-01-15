using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Discounts;
using Dynamicweb.Extensibility.Notifications;
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
            if (Context.Current != null && Context.Current.Session != null)
            {
                var settings = SettingsManager.GetSettingsByShop(Global.CurrentShopId);
                if (settings != null && Global.IsIntegrationActive(settings))
                {
                    if (Convert.ToBoolean(Context.Current.Session["DynamicwebLiveIntegration.OrderExportFailed"]))
                    {
                        Dynamicweb.Notifications.Standard.Page.LoadedArgs loadedArgs = args as Dynamicweb.Notifications.Standard.Page.LoadedArgs;
                        if (loadedArgs?.PageViewInstance?.Template != null)
                        {
                            loadedArgs.PageViewInstance.Template.SetTag("LiveIntegration.OrderExportFailed", true);
                            loadedArgs.PageViewInstance.Template.SetTag("LiveIntegration.FailedOrderID", Convert.ToString(Context.Current.Session["DynamicwebLiveIntegration.FailedOrderId"]));
                        }
                    }                    
                }
            }
        }
    }
}