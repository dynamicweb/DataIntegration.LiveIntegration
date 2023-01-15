﻿using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Cache;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
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
        private bool? FetchProductInfo(Settings settings, ref Product product, double quantity, string variantId,
            string productIdentifier, ResponseCacheLevel productCacheLevel, LiveContext context, Logger logger)
        {
            // If Product is not in the cache, then get it from Erp
            if (!ResponseCache.IsProductInCache(productCacheLevel, productIdentifier, context.User))
            {
                if (!string.IsNullOrEmpty(variantId) && (!settings.GetProductInformationForAllVariants))
                {
                    return null;
                }

                Dictionary<Product, double> products = new Dictionary<Product, double>();

                Product changedProduct = ProductManager.ProductProvider.GetProductFromVariantComboId(product, logger);
                products.Add(changedProduct, quantity);
                // After GetProductFromVariantComboId the variantId is empty or other product is returned so new identifier must be generated
                productIdentifier = ProductManager.ProductProvider.GetProductIdentifier(settings, changedProduct);

                bool fetchedProductInfo = ProductManager.FetchProductInfos(products, context, settings, logger, true);
                if (fetchedProductInfo &&
                    // Check if requested product was not received from response
                    !ResponseCache.IsProductInCache(productCacheLevel, productIdentifier, context.User)
                    )
                {
                    new Logger(settings).Log(ErrorLevel.Error, $"Error receiving product info for product: {ProductManager.ProductProvider.GetProductIdentifier(settings, product)}.");
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
                        && settings.IsLiveIntegrationEnabled
                        && (string.IsNullOrWhiteSpace(settings.ShopId) || settings.ShopId == context.Shop?.Id)
                        && !Global.IsProductLazyLoad(settings)
                        && Connector.IsWebServiceConnectionAvailable(settings, logger))
                    {
                        Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.ProductPriceProvider.PreparePrices START");
                        var productProvider = ProductManager.ProductProvider;

                        var products = new Dictionary<Product, double>();

                        foreach (var selection in selections)
                        {
                            var product = productProvider.GetProductWithUnit(selection.Product, selection.UnitId);
                            if (!products.ContainsKey(product))
                                products.Add(product, selection.Quantity);
                        }
                        LiveContext liveContext = new LiveContext(context);                        
                        if (!ProductManager.FetchProductInfos(products, liveContext, settings, logger))
                        {
                            return;
                        }
                        foreach (var productWithQuantity in products)
                        {
                            var product = productWithQuantity.Key;
                            ProductInfo productInfo = ProductManager.GetProductInfo(product, settings, context.Customer);
                            if (productInfo != null)
                            {
                                ProductManager.ProductProvider.FillProductValues(productInfo, product, settings, productWithQuantity.Value, liveContext);
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

            Diagnostics.ExecutionTable.Current.Add($"DynamicwebLiveIntegration.ProductPriceProvider.FindPriceInfoWithContext product[id='{selection.Product?.Id}' variantId='{selection.Product.VariantId}'] START");

            try
            {
                var productProvider = ProductManager.ProductProvider;
                var productForRequest = productProvider.GetProductWithUnit(selection.Product, selection.UnitId);
                string productIdentifier = productProvider.GetProductIdentifier(settings, productForRequest);
                var productCacheLevel = settings.GetProductCacheLevel();

                var liveContext = new LiveContext(context);
                var logger = new Logger(settings);
                bool? isProductInfoFetched = FetchProductInfo(settings, ref productForRequest, selection.Quantity, selection.VariantId, productIdentifier, productCacheLevel, liveContext, logger);
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
    }
}