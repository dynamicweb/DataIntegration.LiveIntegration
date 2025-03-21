﻿using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Environment;
using Dynamicweb.Extensibility.Notifications;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.NotificationSubscribers
{
    /// <summary>
    /// LiveIntegration in WebApi handle CreateOrder request implemented for #10951
    /// </summary>
    [Subscribe(Ecommerce.Notifications.Ecommerce.Order.AfterSave)]
    public class OrderAfterSave : NotificationSubscriberBase
    {
        public override void OnNotify(string notification, NotificationArgs args)
        {
            if (args != null)
            {
                var myArgs = (Ecommerce.Notifications.Ecommerce.Order.AfterSaveArgs)args;
                if (Context.Current?.Session is object || ExecutingContext.IsBackEnd() ||
                    myArgs.Order == null || myArgs.Order.OrderLines.Count <= 0 ||
                    !myArgs.Order.Complete || myArgs.Order.IsCart || 
                    !string.IsNullOrEmpty(myArgs.Order.IntegrationOrderId))
                {
                    return;
                }
                
                Settings settings = SettingsManager.GetSettingsByShop(myArgs.Order.ShopId);
                var submitType = SubmitType.WebApi;

                if (!EnabledAndActive(settings, submitType))
                {
                    return;
                }

                if (settings != null && Global.EnableCartCommunication(settings, myArgs.Order.Complete)
                    && IsCreateOrderAllowed(myArgs.Order))
                {                    
                    OrderHandler.UpdateOrder(settings, myArgs.Order, submitType);                    
                }
            }
        }
    }
}
