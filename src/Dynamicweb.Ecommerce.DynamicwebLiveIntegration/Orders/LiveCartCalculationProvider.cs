using Dynamicweb.Ecommerce.Cart;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Environment;
using System.Linq;

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
                SubmitType submitType = SubmitType.LiveOrderOrCart;

                if (Global.IsIntegrationActive(settings) &&                    
                    Connector.IsWebServiceConnectionAvailable(settings, submitType))
                {                    
                    if (Global.EnableCartCommunication(settings, cart.Complete))
                    {
                        var contextCurrency = Context.Current?.Request?.GetString("CurrencyCode");
                        if (!string.IsNullOrEmpty(contextCurrency) && !string.Equals(cart.CurrencyCode, contextCurrency, System.StringComparison.OrdinalIgnoreCase))
                        {                            
                            Services.Orders.ForcePriceRecalculation(cart);                            
                        }                                                   
                        bool? result = OrderHandler.UpdateOrder(settings, cart, submitType);
                        return result.HasValue ? result.Value : false;
                    }
                }
                else
                {
                    if (cart.OrderLines.Any(ol => ol.AllowOverridePrices) && cart.DisableDiscountCalculation && cart.AllowOverridePrices)
                    {
                        foreach (var line in cart.OrderLines)
                        {
                            if (line.AllowOverridePrices)
                                line.AllowOverridePrices = false;
                        }                        
                        cart.DisableDiscountCalculation = false;
                        cart.AllowOverridePrices = false;
                        Services.Orders.ForcePriceRecalculation(cart);                        
                    }
                }
            }
            return false;
        }
    }
}
