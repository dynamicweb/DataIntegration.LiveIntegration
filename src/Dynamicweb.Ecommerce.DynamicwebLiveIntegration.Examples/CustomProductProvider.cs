using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Products;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Ecommerce.Products;
using System.Linq;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples
{
    /// <summary>
    /// Class CustomProductProvider.
    /// </summary>
    /// <seealso cref="Products.ProductProviderBase" />
    public class CustomProductProvider : ProductProviderBase
    {        
        /// <summary>
        /// Gets the product identifier.
        /// </summary>
        /// <param name="product">The product.</param>
        /// <param name="unitId">The product unit Id.</param>
        /// <returns>System.String.</returns>
        public override string GetProductIdentifier(Settings settings, Product product, string unitId)
        {
            string unit = string.IsNullOrEmpty(unitId) ? string.Empty : $"-{unitId}";
            return $"{product.Id}-{product.VariantId}-{product.LanguageId}-{product.Number}{unit}";
        }

        /// <summary>
        /// Gets the price.
        /// </summary>
        /// <param name="productInfo">The product info.</param>
        /// <param name="quantity">The quantity.</param>
        /// <param name="product">The product.</param>
        /// <returns>PriceInfo.</returns>
        public override PriceInfo GetPriceInfo(LiveContext context, ProductInfo productInfo, double quantity, Product product)
        {
            // Example: if we have a price per kilogram - we need to multiply it by quantity
            if (double.TryParse(productInfo["TotalPrice"].ToString(), out double unitPriceWithoutVat))
            {
                double? unitPriceWithVat = (double?)productInfo["TotalPriceWithVat"];

                var currency = Common.Context.Currency;
                if (currency is null)
                    currency = Services.Currencies.GetAllCurrencies().FirstOrDefault();
                var price = new PriceInfo(currency);
                price.PriceWithoutVAT = unitPriceWithoutVat * quantity;
                price.PriceWithVAT = unitPriceWithVat != null ? unitPriceWithVat.Value * quantity : 0;
                return price;
            }
            else
            {
                return base.GetPriceInfo(context, productInfo, quantity, product);
            }
        }
    }
}
