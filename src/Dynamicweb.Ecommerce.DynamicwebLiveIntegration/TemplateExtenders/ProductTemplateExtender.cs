using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Products;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Ecommerce.Products;
using Dynamicweb.Environment;
using Dynamicweb.Rendering;
using Dynamicweb.Security.UserManagement;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.TemplateExtenders
{
    /// <summary>
    /// 
    /// </summary>
    public class ProductTemplateExtender : Ecommerce.Products.ProductTemplateExtender
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="template"></param>
        public override void ExtendTemplate(Template template)
        {
            ProductInfo productInfo = null;

            bool pricesLoopExists = template.LoopExists("Product.Prices");
            bool loadCustomFieldsValues = (template.LoopExists("CustomFields") || template.LoopExists("CustomFieldValues"))
                && !template.HtmlHasPartialTag("Ecom:Product.Price")
                && !template.HtmlHasPartialTag("Ecom:Product:Stock");

            if (!pricesLoopExists && !loadCustomFieldsValues)
                return;

            var settings = SettingsManager.GetSettingsByShop(Global.CurrentShopId);
            if (!Global.IsIntegrationActive(settings))
                return;

            var user = Helpers.GetCurrentExtranetUser();
            if (loadCustomFieldsValues)
            {                
                if (settings != null && settings.AddProductFieldsToRequest && Helpers.CanCheckPrice(settings, Product, user))
                {
                    productInfo = FetchProductInfo(settings, Product, user);
                    if (productInfo != null)
                    {
                        ProductManager.FillProductFieldValues(Product, productInfo);
                    }
                }
            }

            if (pricesLoopExists)
            {
                RenderPrices(template, settings, Product, productInfo, user);
            }
        }

        private void RenderPrices(Template template, Settings settings, Product product, ProductInfo productInfo, User user)
        {
            var pricesTemplate = template.GetLoop("Product.Prices");
            if (pricesTemplate != null)
            {
                productInfo = productInfo ?? ProductManager.GetProductInfo(product, settings, user);
                if (productInfo != null)
                {
                    var prices = ProductManager.GetPrices(productInfo);
                    if (prices != null && prices.Count > 0)
                    {
                        Price baseUOMPrice = GetBaseUOMPrice(settings, productInfo, prices);

                        var logger = new Logger(settings);

                        foreach (var price in prices)
                        {
                            PriceContext context = new PriceContext(Common.Context.Currency, Common.Context.Country);
                            var calculated = PriceCalculated.Create(context, new PriceRaw(price.Amount, price.Currency), product);
                            calculated.Calculate();

                            pricesTemplate.SetTag("Ecom:Product.Prices.Amount", calculated.PriceWithoutVATFormattedNoSymbol);
                            pricesTemplate.SetTag("Ecom:Product.Prices.AmountFormatted", calculated.PriceWithoutVATFormatted);
                            pricesTemplate.SetTag("Ecom:Product.Prices.AmountWithVAT", calculated.PriceWithVAT);
                            pricesTemplate.SetTag("Ecom:Product.Prices.AmountWithVATFormatted", calculated.PriceWithVAT);
                            pricesTemplate.SetTag("Ecom:Product.Prices.Currency", price.CurrencyCode);
                            pricesTemplate.SetTag("Ecom:Product.Prices.VariantID", price.VariantId);
                            pricesTemplate.SetTag("Ecom:Product.Prices.UserID", price.UserId);
                            pricesTemplate.SetTag("Ecom:Product.Prices.UserCustomerNumber", price.UserCustomerNumber);
                            pricesTemplate.SetTag("Ecom:Product.Prices.GroupID", price.UserGroupId);
                            pricesTemplate.SetTag("Ecom:Product.Prices.ValidFrom", price.ValidFrom.HasValue ? price.ValidFrom.Value.ToShortDateString() : String.Empty);
                            pricesTemplate.SetTag("Ecom:Product.Prices.ValidTo", price.ValidTo.HasValue ? price.ValidTo.Value.ToShortDateString() : String.Empty);                            
                            pricesTemplate.SetTag("Ecom:Product.Prices.UnitID", price.UnitId);
                            pricesTemplate.SetTag("Ecom:Product.Prices.LanguageID", price.LanguageId);
                            pricesTemplate.SetTag("Ecom:Product.Prices.Country", price.CountryCode);
                            pricesTemplate.SetTag("Ecom:Product.Prices.ShopID", price.ShopId);
                            pricesTemplate.SetTag("Ecom:Product.Prices.Quantity", price.Quantity);

                            pricesTemplate.SetTag("Ecom:Product.Prices.Price", calculated.Price);
                            pricesTemplate.SetTag("Ecom:Product.Prices.PricePIP", calculated.PricePIP);
                            pricesTemplate.SetTag("Ecom:Product.Prices.PriceFormatted", calculated.PriceFormatted);
                            pricesTemplate.SetTag("Ecom:Product.Prices.PriceWithVAT", calculated.PriceWithVAT);
                            pricesTemplate.SetTag("Ecom:Product.Prices.PriceWithVATFormatted", calculated.PriceWithVATFormatted);
                            pricesTemplate.SetTag("Ecom:Product.Prices.PriceWithoutVAT", calculated.PriceWithoutVAT);
                            pricesTemplate.SetTag("Ecom:Product.Prices.PriceWithoutVATFormatted", calculated.PriceWithoutVATFormatted);
                            pricesTemplate.SetTag("Ecom:Product.Prices.PriceWithoutVATFormatted", calculated.PriceWithoutVATFormatted);
                            pricesTemplate.SetTag("Ecom:Product.Prices.Price.VAT", calculated.VAT);
                            pricesTemplate.SetTag("Ecom:Product.Prices.Price.VATFormatted", calculated.VATFormatted);
                            pricesTemplate.SetTag("Ecom:Product.Prices.Price.VATPercent", calculated.VATPercent);
                            pricesTemplate.SetTag("Ecom:Product.Prices.Price.VATPercentFormatted", calculated.VATPercentFormatted);
                            pricesTemplate.SetTag("Ecom:Product.Prices.Currency.Code", calculated.Currency.Code);
                            pricesTemplate.SetTag("Ecom:Product.Prices.Currency.IsDefault", calculated.Currency.IsDefault);
                            pricesTemplate.SetTag("Ecom:Product.Prices.Currency.Name", calculated.Currency.GetName(Common.Context.LanguageID));
                            pricesTemplate.SetTag("Ecom:Product.Prices.Currency.PayGatewayCode", calculated.Currency.PayGatewayCode);
                            pricesTemplate.SetTag("Ecom:Product.Prices.Currency.Symbol", calculated.Currency.Symbol);
                            pricesTemplate.SetTag("Ecom:Product.Prices.Currency.SymbolPlace", calculated.Currency.SymbolPlace);
                            pricesTemplate.SetTag("Ecom:Product.Prices.Currency.Rate", calculated.Currency.Rate);

                            if (!string.IsNullOrEmpty(calculated.Currency.CultureInfo))
                            {
                                var culture = Helpers.GetCultureInfo(calculated.Currency.CultureInfo, logger);
                                if (culture == null)
                                {
                                    culture = ExecutingContext.GetCulture();
                                }
                                pricesTemplate.SetTag("Ecom:Product.Prices.Currency.IntegerSeparator", culture.NumberFormat.CurrencyGroupSeparator);
                                pricesTemplate.SetTag("Ecom:Product.Prices.Currency.DecimalSeparator", culture.NumberFormat.CurrencyDecimalSeparator);
                            }
                            pricesTemplate.SetTag("Ecom:Product.Prices.CurrencyName", calculated.Currency.GetName(Common.Context.LanguageID));
                            pricesTemplate.SetTag("Ecom:Product.Prices.CurrencyRate", calculated.Currency.Rate);
                            pricesTemplate.SetTag("Ecom:Product.Prices.CurrencyCode", calculated.Currency.Code);

                            if (baseUOMPrice != null)
                            {
                                pricesTemplate.SetTag("Ecom:Product.Prices.Stock", GetStockFromBaseUOM(baseUOMPrice.Quantity, Product.Stock, price.Quantity));
                            }
                            pricesTemplate.CommitLoop();
                        }
                    }
                }
            }
        }

        private ProductInfo FetchProductInfo(Settings settings, Product product, User user)
        {
            var productInfo = ProductManager.GetProductInfo(product, settings, user);
            if (productInfo == null)
            {
                Diagnostics.ExecutionTable.Current.Add($"ProductTemplateExtender FetchProductInfos product[id='{product?.Id}' variantId='{product.VariantId}'] START");
                var products = new Dictionary<Product, double>();
                products.Add(Product, 1);
                var context = new LiveContext(Helpers.GetCurrentCurrency(), user, Services.Shops.GetShop(Global.CurrentShopId));
                if (ProductManager.FetchProductInfos(products, context, settings, new Logger(settings), false))
                {
                    productInfo = ProductManager.GetProductInfo(product, settings, user);
                }
                Diagnostics.ExecutionTable.Current.Add($"ProductTemplateExtender FetchProductInfos product[id='{product?.Id}' variantId='{Product.VariantId}'] END");
            }
            return productInfo;
        }

        private Price GetBaseUOMPrice(Settings settings, ProductInfo productInfo, List<Price> prices)
        {
            Price price = null;
            if (settings.UseUnitPrices && prices != null && productInfo["ProductDefaultUnitId"] != null)
            {
                string defaultUnitId = productInfo["ProductDefaultUnitId"].ToString();
                if (!string.IsNullOrEmpty(defaultUnitId))
                {
                    price = prices.FirstOrDefault(p => p != null && string.Equals(p.UnitId, defaultUnitId, StringComparison.OrdinalIgnoreCase));
                }
            }
            return price;
        }

        private double GetStockFromBaseUOM(double baseUOMquantity, double baseStock, double uomQuantity)
        {
            if (uomQuantity != 0)
            {
                return (baseStock * baseUOMquantity) / uomQuantity;
            }
            return 0;
        }
    }
}
