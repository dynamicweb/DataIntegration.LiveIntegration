using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Ecommerce.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Products
{
    /// <summary>
    /// Default implementation of a product provider to get prices and other info for products from the ERP.
    /// Inherit this class and override one or more of its methods to alter the behavior.
    /// </summary>
    /// <example>
    /// <code description="Example" source="..\..\Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples\CustomProductProvider.cs" lang="CS"></code>
    /// </example>
    public class ProductProviderBase
    {
        /// <summary>
        /// Creates a unique product identifier by concatenating the product ID or number (depends on the CalculatePriceUsingProductNumber setting), the variant ID and the language ID.
        /// Override to build up your own unique identifier.
        /// </summary>
        /// <param name="product">The product.</param>
        /// <returns>System.String.</returns>
        public virtual string GetProductIdentifier(Settings settings, Product product)
        {
            return GetProductIdentifier(settings, product, null);
        }

        /// <summary>
        /// Creates a unique product identifier by concatenating the product ID or number (depends on the CalculatePriceUsingProductNumber setting), the variant ID and the language ID.
        /// Override to build up your own unique identifier.
        /// </summary>
        /// <param name="product">The product.</param>
        /// <param name="unitId">The product unit Id.</param>
        /// <returns>System.String.</returns>
        public virtual string GetProductIdentifier(Settings settings, Product product, string unitId)
        {
            string unit = string.Empty;
            if (!string.IsNullOrEmpty(unitId) && settings != null && settings.UseUnitPrices)
            {
                unit = $".{unitId}";
            }
            if (settings != null && settings.CalculatePriceUsingProductNumber)
            {
                return $"{product.Number}.{GetProductVariantIdIdentifier(product)}.{product.LanguageId}{unit}";
            }
            else
            {
                return $"{product.Id}.{GetProductVariantIdIdentifier(product)}.{product.LanguageId}{unit}";
            }
        }

        /// <summary>
        /// Creates a product VariantId identifier.
        /// </summary>
        /// <param name="product">The product.</param>
        /// <returns>Empty string.</returns>
        public virtual string GetProductVariantIdIdentifier(Product product)
        {
            if (string.IsNullOrEmpty(product.VariantId))
            {
                return string.Empty;
            }
            else
            {
                return product.VariantId;
            }
        }

        /// <summary>
        /// Gets product object with DefaultUnitId set to supplied unit id if the unit exists
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="unitId">Exising product unid id</param>
        /// <returns></returns>
        public virtual Product GetProductWithUnit(Product product, string unitId)
        {
            var result = product;
            if (product.IsUnitUpdateNeeded(unitId))
            {
                result = Services.Products.Clone(product);
                result.DefaultUnitId = unitId;
            }
            return result;
        }

        /// <summary>
        /// Gets the price.
        /// </summary>
        /// <param name="product">The product.</param>
        /// <param name="quantity">The quantity.</param>
        /// <returns>PriceInfo</returns>
        /// <exception cref="ArgumentNullException">product</exception>
        /// <example>
        /// <code description="Overriding example" source="..\..\Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples\CustomProductProvider.cs" lang="CS"></code>
        /// </example>
        public virtual PriceInfo GetPriceInfo(LiveContext context, ProductInfo product, double quantity)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

            double? priceWithoutVat = (double?)product["TotalPrice"];
            double? priceWithVat = (double?)product["TotalPriceWithVat"];

            if (!priceWithoutVat.HasValue)
            {
                if (quantity <= 0)
                {
                    quantity = 1;
                }

                var erpPriceResponse = ((IList<ProductPrice>)product["Prices"] ?? Enumerable.Empty<ProductPrice>())
                       .Where(p => quantity >= p.Quantity.GetValueOrDefault(1))
                       .OrderByDescending(p => p.Quantity.GetValueOrDefault(1))
                       .FirstOrDefault();

                priceWithoutVat = erpPriceResponse?.Amount;
                priceWithVat = erpPriceResponse?.AmountWithVat;
            }

            PriceInfo result = context.PriceContext != null ? new PriceInfo(context.PriceContext.Currency) : new PriceInfo(context.Currency);
            result.PriceWithoutVAT = priceWithoutVat != null ? priceWithoutVat.Value : 0;
            if (priceWithVat != null)
            {
                result.PriceWithVAT = priceWithVat.Value;
                if (result.PriceWithVAT >= result.PriceWithoutVAT && result.PriceWithoutVAT > 0)
                {
                    //This is to handle the case where the ERP returns VAT and VAT percent, but DW is setup to i.e. 0% vat.
                    result.VAT = result.PriceWithVAT - result.PriceWithoutVAT;
                }
            }
            else
            {
                var price = new PriceRaw
                {
                    Price = result.PriceWithoutVAT,
                    Currency = context.Currency
                };
                PriceContext priceContext = new PriceContext(context.Currency, context.Country);
                var calculated = PriceCalculated.Create(priceContext, price);
                if (calculated != null)
                {
                    calculated.Calculate();
                    result.PriceWithVAT = calculated.PriceWithVAT;
                }
            }

            return result;
        }

        /// <summary>
        /// Fills the product values.
        /// </summary>
        /// <param name="productInfo">The product information.</param>
        /// <param name="product">The product.</param>
        /// <param name="quantity">The quantity.</param>
        public virtual void FillProductValues(ProductInfo productInfo, Product product, Settings settings, double quantity, LiveContext context)
        {
            var price = GetPriceInfo(context, productInfo, quantity);            
            PriceInfo productPrice = PriceManager.GetPrice(context.PriceContext ?? new PriceContext(context.Currency, context.Country), product);
            productPrice.PriceWithoutVAT = price.PriceWithoutVAT;
            productPrice.PriceWithVAT = price.PriceWithVAT;

            // Update Product Custom Fields
            if (settings.AddProductFieldsToRequest && product.ProductFieldValues.Count > 0)
            {
                FillProductFieldValues(product, productInfo);
            }
        }

        /// <summary>
        /// Gets the product prices list.
        /// </summary>        
        /// <param name="productInfo">The product information.</param>
        public virtual List<Price> GetPrices(ProductInfo productInfo)
        {
            var result = new List<Price>();
            var prices = (IList<ProductPrice>)productInfo["Prices"];

            foreach (var price in prices.OrderBy(p => p.Quantity.GetValueOrDefault()))
            {
                result.Add(new Price
                {
                    Id = price.Id,
                    Amount = price.Amount.GetValueOrDefault(),
                    Quantity = price.Quantity.GetValueOrDefault(),
                    ProductId = price.ProductId,
                    VariantId = price.ProductVariantId,
                    LanguageId = Common.Context.LanguageID,
                    UserCustomerNumber = price.UserCustomerNumber,
                    IsInformative = false,
                    CurrencyCode = Common.Context.Currency.Code,
                    UnitId = price.UnitId
                });
            }
            return result;
        }

        /// <summary>
        /// Extracts the prices.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns>IList&lt;ProductPrice&gt;.</returns>
        public virtual IList<ProductPrice> ExtractPrices(Settings settings, XmlDocument response, Logger logger)
        {
            var prices = new List<ProductPrice>();

            var pricesXml = response.SelectNodes("//item [@table='EcomPrices']");

            if (pricesXml != null)
            {
                foreach (XmlNode priceXml in pricesXml)
                {
                    string customerNumber = priceXml.SelectSingleNode("column [@columnName='PriceUserCustomerNumber']")?.InnerText;
                    if (string.Equals(customerNumber, settings.AnonymousUserKey, StringComparison.OrdinalIgnoreCase))
                    {
                        customerNumber = string.Empty;
                    }

                    var price = new ProductPrice
                    {
                        Id = priceXml.SelectSingleNode("column [@columnName='PriceId']")?.InnerText,
                        ProductId = priceXml.SelectSingleNode("column [@columnName='PriceProductId']")?.InnerText,
                        ProductVariantId = priceXml.SelectSingleNode("column [@columnName='PriceProductVariantId']")?.InnerText,
                        UserCustomerNumber = customerNumber,
                    };

                    var node = priceXml.SelectSingleNode("column [@columnName='PriceAmount']");
                    price.Amount = (node != null && !string.IsNullOrEmpty(node.InnerText)) ? Helpers.ToDouble(settings, logger, node.InnerText) : (double?)null;

                    node = priceXml.SelectSingleNode("column [@columnName='PriceAmountWithVat']");
                    price.AmountWithVat = (node != null && !string.IsNullOrEmpty(node.InnerText)) ? Helpers.ToDouble(settings, logger, node.InnerText) : (double?)null;

                    node = priceXml.SelectSingleNode("column [@columnName='PriceQuantity']");
                    price.Quantity = (node != null && !string.IsNullOrEmpty(node.InnerText)) ? Helpers.ToDouble(settings, logger, node.InnerText) : (double?)null;

                    price.UnitId = priceXml.SelectSingleNode("column [@columnName='PriceProductUnitId']")?.InnerText;

                    prices.Add(price);
                }
            }

            return prices;
        }

        /// <summary>
        /// Checks if the product is enabled for the live prices requests.
        /// Override this method if you want to skip some products from being looked up in the ERP.
        /// </summary>
        /// <param name="product">The product</param>
        /// <returns>True if product is enabled for the live prices, otherwise false.</returns>
        public virtual bool IsLivePriceEnabledForProduct(Product product)
        {
            return true;
        }

        /// <summary>
        /// Gets the product from variant combo identifier.
        /// </summary>
        /// <param name="product">The product.</param>
        /// <returns>Product.</returns>
        public virtual Product GetProductFromVariantComboId(Product product, Logger logger)
        {
            try
            {
                // If master: exchange master product with default variant
                if (Services.Products.IsVariantMaster(product) && !string.IsNullOrEmpty(product.DefaultVariantComboId))
                {
                    Product returnProduct = Services.Products.GetProductById(product.Id, product.DefaultVariantComboId, product.LanguageId);
                    return returnProduct;
                }
                else
                {
                    return product;
                }
            }
            catch (Exception e)
            {
                logger.Log(ErrorLevel.Error, $"Failed getting product by method GetProductFromVariantComboId: '{e.Message}'.");
                return product;
            }
        }

        public virtual void FillProductFieldValues(Product product, ProductInfo productInfo)
        {
            foreach (ProductFieldValue pfv in product.ProductFieldValues)
            {
                if (productInfo.ContainsKey(pfv.ProductField.SystemName))
                {
                    pfv.Value = productInfo[pfv.ProductField.SystemName];
                }
            }
        }
    }
}