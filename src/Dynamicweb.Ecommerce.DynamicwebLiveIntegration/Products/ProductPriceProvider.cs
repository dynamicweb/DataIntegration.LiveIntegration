using Dynamicweb.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Cache;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Ecommerce.Products;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Products
{
    /// <summary>
    /// PriceProvider to find prices and other information for products.
    /// </summary>
    /// <seealso cref="PriceProvider" />
    public class ProductPriceProvider : PriceProvider, IPriceInfoProvider
    {        
        private static bool? FetchProductInfo(Settings settings, PriceProductSelection product,
            string productIdentifier, ResponseCacheLevel productCacheLevel, LiveContext context, Logger logger)
        {
            // If Product is not in the cache, then get it from Erp
            if (!ResponseCache.IsProductInCache(productCacheLevel, productIdentifier, context.User, context.Currency))
            {
                if (!string.IsNullOrEmpty(product.VariantId) && (!settings.GetProductInformationForAllVariants))
                {
                    return null;
                }

                List<PriceProductSelection> products = new List<PriceProductSelection>();

                bool showVariantDefault = SystemConfiguration.Instance.GetBoolean("/Globalsettings/Ecom/Product/ShowVariantDefault");

                Product productForRequest = showVariantDefault ? ProductManager.ProductProvider.GetProductFromVariantComboId(product.Product, logger) : product.Product;
                products.Add(productForRequest.GetPriceProductSelection(product.Quantity, product.UnitId));
                // After GetProductFromVariantComboId the variantId is empty or other product is returned so new identifier must be generated
                productIdentifier = ProductManager.ProductProvider.GetProductIdentifier(settings, productForRequest, product.UnitId);

                bool fetchedProductInfo = ProductManager.FetchProductInfos(products, context, settings, logger, true, SubmitType.Live);
                if (fetchedProductInfo &&
                    // Check if requested product was not received from response
                    !ResponseCache.IsProductInCache(productCacheLevel, productIdentifier, context.User, context.Currency)
                    )
                {
                    new Logger(settings).Log(ErrorLevel.Error, $"Error receiving product info for product: {ProductManager.ProductProvider.GetProductIdentifier(settings, product.Product, product.UnitId)}.");
                    return null;
                }
                return fetchedProductInfo;
            }
            return true;
        }

        /// <summary>
        /// Prepares the prices.
        /// </summary>        
        public override void PreparePrices(PriceContext context, IEnumerable<PriceProductSelection> selections)
        {
            if (selections != null && selections.Any())
            {
                var settings = SettingsManager.GetSettingsByShop(context.Shop?.Id);
                if (settings != null)
                {
                    var logger = new Logger(settings);

                    if (settings.EnableLivePrices &&
                        (settings.LiveProductInfoForAnonymousUsers || context.Customer != null) &&
                        (context.Customer == null || !context.Customer.IsLivePricesDisabled)
                        && Global.IsIntegrationActive(settings)
                        && (string.IsNullOrWhiteSpace(settings.ShopId) || settings.ShopId == context.Shop?.Id)
                        && !Global.IsProductLazyLoad(settings)
                        && Connector.IsWebServiceConnectionAvailable(settings, logger, SubmitType.Live))
                    {
                        Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.ProductPriceProvider.PreparePrices START");
                        var productProvider = ProductManager.ProductProvider;

                        var products = new List<PriceProductSelection>();

                        foreach (var selection in selections)
                        {                            
                            if (!products.Any(p => p.Product == selection.Product))
                                products.Add(selection);
                        }
                        LiveContext liveContext = new LiveContext(context);                        
                        if (!ProductManager.FetchProductInfos(products, liveContext, settings, logger, true, SubmitType.Live))
                        {
                            return;
                        }
                        foreach (var productWithQuantity in products)
                        {
                            var product = productWithQuantity.Product;
                            ProductInfo productInfo = ProductManager.GetProductInfo(product, settings, context.Customer, liveContext, productWithQuantity.UnitId);
                            if (productInfo != null)
                            {
                                ProductManager.ProductProvider.FillProductValues(productInfo, product, settings, productWithQuantity.Quantity, liveContext);
                            }
                        }
                        Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.ProductPriceProvider.PreparePrices END");
                    }
                }
            }
        }
        
        public PriceInfo FindPriceInfo(PriceContext context, PriceProductSelection selection)
        {
            var settings = SettingsManager.GetSettingsByShop(context.Shop?.Id);
            if (settings == null || !Helpers.CanCheckPrice(settings, selection.Product, context.Customer))
            {
                return null;
            }

            Diagnostics.ExecutionTable.Current.Add($"DynamicwebLiveIntegration.ProductPriceProvider.FindPriceInfoWithContext product[id='{selection.Product?.Id}' variantId='{selection.Product.VariantId}' unitId='{selection.UnitId}'] START");

            try
            {
                var productProvider = ProductManager.ProductProvider;                
                string productIdentifier = productProvider.GetProductIdentifier(settings, selection.Product, selection.UnitId);
                var productCacheLevel = settings.GetProductCacheLevel();

                var liveContext = new LiveContext(context);
                var logger = new Logger(settings);
                bool? isProductInfoFetched = FetchProductInfo(settings, selection, productIdentifier, productCacheLevel, liveContext, logger);
                if (isProductInfoFetched == null || !isProductInfoFetched.Value)
                {
                    return null;
                }

                var productCache = ResponseCache.GetProductInfos(productCacheLevel, context.Customer);

                ProductInfo productInfo = productCache != null && productCache.TryGetValue(productIdentifier, out productInfo)
                    ? productInfo
                    : null;

                return productInfo != null ? productProvider.GetPriceInfo(liveContext, productInfo, selection.Quantity) : null;
            }
            catch (Exception e)
            {
                new Logger(settings).Log(ErrorLevel.Error, $"Unknown error during FindPriceInfoWithContext(). Exception: {e.Message}");
                return null;
            }
            finally
            {
                Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.ProductPriceProvider.FindPriceInfoWithContext END");
            }
        }
        
        IEnumerable<KeyValuePair<PriceQuantityInfo, PriceInfo>> IPriceInfoProvider.FindQuantityPriceInfos(PriceContext context, Product product)
        {
            var settings = SettingsManager.GetSettingsByShop(context.Shop?.Id);
            if (settings is null || !Helpers.CanCheckPrice(settings, product, context.Customer))
                return null;

            var result = new List<KeyValuePair<PriceQuantityInfo, PriceInfo>>();
            var unitPrices = ProductViewModelExtensions.GetUnitPrices(settings, context.Customer, product);
            foreach (var unitPrice in unitPrices)
            {
                result.Add(new KeyValuePair<PriceQuantityInfo, PriceInfo>(
                    new PriceQuantityInfo()
                    {
                        Quantity = unitPrice.Quantity ?? 0,
                        UnitId = unitPrice.UnitId,
                    },
                    ProductProviderBase.GetPriceInfo(context, unitPrice.Amount, unitPrice.AmountWithVat)
                ));
            }
            return result;
        }
    }
}