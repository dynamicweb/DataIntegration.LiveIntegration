using Dynamicweb.Ecommerce.Cart;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Environment;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Orders
{
    public class LiveCartCalculationProvider : CartCalculationProvider
    {
        public override bool CalculateCart(Order cart)
        {
            if (cart != null && cart.OrderLines.Count > 0 &&
                !(cart.Complete && cart.IsExported))
            {                
                var settings = SettingsManager.GetSettingsByShop(cart.ShopId);

                if (Global.IsIntegrationActive(settings) &&                    
                    Connector.IsWebServiceConnectionAvailable(settings))
                {                    
                    if (Global.EnableCartCommunication(settings, cart.Complete))
                    {
                        var contextCurrency = Context.Current?.Request?.GetString("CurrencyCode");
                        if (!string.IsNullOrEmpty(contextCurrency) && !string.Equals(cart.CurrencyCode, contextCurrency, System.StringComparison.OrdinalIgnoreCase))
                        {                            
                            Services.Orders.ForcePriceRecalculation(cart);                            
                        }                                                   
                        bool? result = OrderHandler.UpdateOrder(settings, cart, SubmitType.LiveOrderOrCart);
                        return result.HasValue ? result.Value : false;
                    }
                }
            }
            return false;
        }
    }
}
