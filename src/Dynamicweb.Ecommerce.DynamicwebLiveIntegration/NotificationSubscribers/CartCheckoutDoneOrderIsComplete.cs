using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Extensibility.Notifications;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.NotificationSubscribers
{
    /// <summary>
    /// Sends the order to the ERP whe it's complete.
    /// </summary>
    /// <seealso cref="NotificationSubscriberBase" />
    [Subscribe(Ecommerce.Notifications.Ecommerce.Cart.CheckoutDoneOrderIsComplete)]
    public class CartCheckoutDoneOrderIsComplete : NotificationSubscriberBase
    {
        /// <summary>
        /// Handles the notification.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="args">The args.</param>
        public override void OnNotify(string notification, NotificationArgs args)
        {
            if (args != null)
            {
                var myArgs = (Ecommerce.Notifications.Ecommerce.Cart.CheckoutDoneOrderIsCompleteArgs)args;
                if (myArgs.Order == null || myArgs.Order.OrderLines.Count <= 0)
                {
                    return;
                }
                
                Settings settings = SettingsManager.GetSettingsByShop(myArgs.Order.ShopId);
                var submitType = SubmitType.LiveOrderOrCart;

                if (!EnabledAndActive(settings, submitType))
                {
                    return;
                }

                if (settings != null && Global.EnableCartCommunication(settings, myArgs.Order.Complete))
                {
                    if (myArgs.Order.Complete && myArgs.Order.Currency != Common.Context.Currency)
                    {
                        // This can happen when redirected back from some CheckOut handler
                        // Common.Context.Currency can be set to the default Application currency,
                        // while the completed order can be in another currency
                        Common.Context.Currency = myArgs.Order.Currency;
                    }

                    if (!IsCreateOrderAllowed(myArgs.Order))
                    {
                        return;
                    }

                    bool? result = OrderHandler.UpdateOrder(settings, myArgs.Order, submitType);

                    // clear cached prices to update stock if order is completed with success
                    if (result.HasValue && result.Value)
                    {
                        SetProductInformation(settings, myArgs.Order);
                    }
                }
            }
        }
    }
}