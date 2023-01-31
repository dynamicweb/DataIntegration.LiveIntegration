using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
using Dynamicweb.Extensibility.Notifications;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.NotificationSubscribers
{
    /// <summary>
    /// Adds a global template tag to indicate the status of the web service connection.
    /// </summary>
    /// <seealso cref="NotificationSubscriberBase" />
    [Subscribe(Dynamicweb.Notifications.Standard.Page.OnGlobalTags)]
    public class PageOnGlobalTags : NotificationSubscriberBase
    {
        /// <summary>
        /// Handles the notification.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="args">The args.</param>
        public override void OnNotify(string notification, NotificationArgs args)
        {
            Dynamicweb.Notifications.Standard.Page.PageviewNotificationArgs pageviewNotificationArgs = args as Dynamicweb.Notifications.Standard.Page.PageviewNotificationArgs;
            if (pageviewNotificationArgs != null)
            {                
                var settings = SettingsManager.GetSettingsByShop(Global.GetShopId(pageviewNotificationArgs.Pageview));
                if (settings != null && EnabledAndActive(settings))
                {
                    string globalTagName = settings.WebServiceConnectionStatusGlobalTagName;
                    if (!string.IsNullOrEmpty(globalTagName))
                    {
                        if (pageviewNotificationArgs.Template.TagExists(globalTagName))
                        {
                            pageviewNotificationArgs.Template.SetTag(globalTagName, Connector.IsWebServiceConnectionAvailable(settings).ToString().ToLower());
                            pageviewNotificationArgs.Template.SetTag("Global:LiveIntegration.IsLazyLoadingForProductInfoEnabled", Global.IsLazyLoadingForProductInfoEnabled(settings).ToString().ToLower());
                        }
                    }
                }
            }
        }
    }
}