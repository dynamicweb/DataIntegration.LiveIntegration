using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.NotificationSubscribers;
using Dynamicweb.Extensibility.Notifications;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Shipping
{
    [Subscribe(Ecommerce.Notifications.Ecommerce.Cart.BeforeShippingCalculation)]
    public class CartBeforeShippingCalculation : NotificationSubscriberBase
    {
        public override void OnNotify(string notification, NotificationArgs args)
        {
            if (args is null || Context.Current?.Items is null)
                return;

            var myArgs = (Ecommerce.Notifications.Ecommerce.Cart.BeforeShippingCalculationArgs)args;
            if (myArgs.Shipping is null || myArgs.Order is null || Context.Current.Session?["ErpShippingFeeProvider_" + myArgs.Order.Id] is null)
            {
                return;
            }
            Context.Current.Items[$"CartBeforeShippingCalculationShippingId{myArgs.Order.Id}"] = myArgs.Shipping.Id;
        }
    }
}
