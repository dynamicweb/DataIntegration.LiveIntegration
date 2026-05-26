using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.NotificationSubscribers;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Extensibility.Notifications;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Shipping
{
    [Subscribe(Ecommerce.Notifications.Ecommerce.Cart.AfterShippingCalculation)]
    public class CartAfterShippingCalculation : NotificationSubscriberBase
    {
        public override void OnNotify(string notification, NotificationArgs args)
        {
            if (args is null || Context.Current?.Items is null)
                return;

            var calculationArgs = (Ecommerce.Notifications.Ecommerce.Cart.AfterShippingCalculationArgs)args;
            if (string.IsNullOrEmpty(calculationArgs.Shipping?.Id) || calculationArgs.Order is null || Context.Current.Session?[ErpShippingFeeProvider.OrderMarkerKey(calculationArgs.Order.Id)] is null)
            {
                return;
            }

            var cached = Context.Current.Session[ErpShippingFeeProvider.MethodCacheKey(calculationArgs.Order.Id, calculationArgs.Shipping?.Id)] as PriceInfo;
            if (cached is null)
                return;

            calculationArgs.Price.PriceWithVAT = cached.PriceWithVAT;
            calculationArgs.Price.PriceWithoutVAT = cached.PriceWithoutVAT;
            calculationArgs.Price.VAT = cached.VAT;
            calculationArgs.Price.VATPercent = cached.VATPercent;
        }
    }
}
