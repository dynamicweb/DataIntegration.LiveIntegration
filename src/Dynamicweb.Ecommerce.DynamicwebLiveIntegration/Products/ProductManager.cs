using Dynamicweb.Caching;
using Dynamicweb.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Cache;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Ecommerce.Products;
using Dynamicweb.Extensibility.AddIns;
using Dynamicweb.Security.UserManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Products
{
    /// <summary>
    /// Static manager to handle product info requests.
    /// </summary>
    public static class ProductManager
    {
        private static readonly string ProductProviderCacheKey = $"{Constants.AssemblyVersion}DynamicwebLiveIntegrationProductProvider";

        /// <summary>
        /// Gets the product provider. Implement your own class deriving ProductProviderBase if you want to override the built-in behavior.
        /// </summary>
        /// <value>The product provider.</value>
        internal static ProductProviderBase ProductProvider
        {
            get
            {
                ProductProviderBase provider;
                try
                {
                    Caching.Cache.Current.TryGet(ProductProviderCacheKey, out provider);                                        
                }
                catch
                {
                    provider = new ProductProviderBase();
                    Caching.Cache.Current[ProductProviderCacheKey] = provider;                    
                }
                if (provider is null)
                {
                    foreach (Type addIn in AddInManager.GetTypes(typeof(ProductProviderBase)))
                    {
                        if (!ReferenceEquals(addIn, typeof(ProductProviderBase)))
                        {
                            provider = (ProductProviderBase)AddInManager.GetInstance(addIn);
                            break;
                        }
                    }

                    if (provider is null)
                    {
                        provider = new ProductProviderBase();
                    }

                    Caching.Cache.Current[ProductProviderCacheKey] = provider;
                }
                return provider;
            }
        }

        /// <summary>
        /// Fills the product values from the response.
        /// </summary>
        /// <param name="product">The product.</param>
        /// <param name="quantity">The quantity.</param>
        public static void FillProductValues(Settings settings, Product product, double quantity, LiveContext context)
        {
            ProductInfo productInfo = GetProductInfo(product, settings, context.User);
            if (productInfo != null)
            {
                Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.ProductManager.FillProductValues START");
                ProductProvider.FillProductValues(productInfo, product, settings, quantity, context);
                Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.ProductManager.FillProductValues END");
            }
        }

        public static void FillProductFieldValues(Product product, ProductInfo productInfo)
        {
            ProductProvider.FillProductFieldValues(product, productInfo);
        }

        /// <summary>
        /// Gets the product prices list from the response.
        /// </summary>
        /// <param name="productInfo">The product info.</param>
        public static List<Price> GetPrices(ProductInfo productInfo)
        {
            List<Price> prices = new List<Price>();
            if (productInfo != null)
            {
                prices = ProductProvider.GetPrices(productInfo);
            }
            return prices;
        }

        /// <summary>
        /// Fetches the product info from the ERP.
        /// </summary>
        /// <param name="products">The products with quantities.</param>
        /// <param name="user">The user.</param>
        /// <param name="updateCache">Update cache with products information retrieved.</param>
        /// <returns><c>true</c> if product information was fetched, <c>false</c> otherwise.</returns>
        internal static bool FetchProductInfos(Dictionary<Product, double> products, LiveContext context, Settings settings, Logger logger, bool doCurrencyCheck, bool updateCache = true)
        {
            if (products == null || products.Count == 0)
            {
                return false;
            }

            settings = settings ?? SettingsManager.GetSettingsByShop(context.Shop?.Id);
            if (!settings.EnableLivePrices)
            {
                string cacheKey = "DynamicwebLiveIntegration.LivePricesDisabled";
                if (!Caching.Cache.Current.Contains(cacheKey))
                {
                    Caching.Cache.Current.Set(cacheKey, string.Empty, new CacheItemPolicy { AbsoluteExpiration = DateTimeOffset.Now.AddDays(1) });
                    logger.Log(ErrorLevel.DebugInfo, "Live Prices are Disabled");
                }
                return false;
            }

            if (!Connector.IsWebServiceConnectionAvailable(settings, logger))
            {
                logger.Log(ErrorLevel.DebugInfo, "Live Prices are unavailable");
                return false;
            }

            if (context.User == null && !settings.LiveProductInfoForAnonymousUsers)
            {
                logger.Log(ErrorLevel.DebugInfo, "No user is currently logged in. Anonymous user is not allowed.");
                return false;
            }

            if (context.User != null && context.User.IsLivePricesDisabled)
            {
                logger.Log(ErrorLevel.DebugInfo, $"Calculated prices are not allowed for the user '{context.User.UserName}'.");
                return false;
            }

            var productCacheLevel = settings.GetProductCacheLevel();
            // Check for existence of the given products in the Cache
            Dictionary<Product, double> productsForRequest = GetProductsForRequest(settings, productCacheLevel, products, logger, context, doCurrencyCheck);

            if (productsForRequest.Count == 0)
            {
                return true;
            }

            if (settings.MaxProductsPerRequest == 0)
            {
                // don't split requests
                return FetchProductInfosInternal(productsForRequest, productCacheLevel, context, settings, logger, updateCache);
            }

            var allProductsSuccessfullyFetched = true;
            var numberOfRequests = Core.Converter.ToInt32(Math.Ceiling((double)productsForRequest.Count / (double)settings.MaxProductsPerRequest));

            for (var i = 0; i < numberOfRequests; i++)
            {
                var currentSetFetched = FetchProductInfosInternal(productsForRequest.OrderBy(pair => pair.Key?.Id).Skip(i * settings.MaxProductsPerRequest).Take(settings.MaxProductsPerRequest).ToDictionary(pair => pair.Key, pair => pair.Value), productCacheLevel, context, settings, logger, updateCache);
                allProductsSuccessfullyFetched = allProductsSuccessfullyFetched ? currentSetFetched : allProductsSuccessfullyFetched;
            }

            return allProductsSuccessfullyFetched;
        }

        private static bool FetchProductInfosInternal(Dictionary<Product, double> productsForRequest, ResponseCacheLevel productCacheLevel, LiveContext context, Settings settings, Logger logger, bool updateCache = true)
        {
            Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.ProductManager.FetchProductInfos START");
            string request = BuildProductRequest(settings, productsForRequest, context, logger);

            string requestHash = null;
            XmlDocument response = null;
            bool isResponseCached = false;
            if (productCacheLevel == ResponseCacheLevel.Page)
            {
                requestHash = Helpers.CalculateHash(request);
                isResponseCached = Caching.Cache.Current.TryGet(requestHash, out response);
            }

            HttpStatusCode httpStatusCode = HttpStatusCode.OK;
            if (!isResponseCached)
            {
                response = Connector.GetProductsInfo(settings, request, logger, out httpStatusCode, settings.MakeRetryForLiveProductInformation);
            }

            bool result = true;
            if (response != null && !string.IsNullOrEmpty(response.InnerXml))
            {
                if (productCacheLevel == ResponseCacheLevel.Page && !string.IsNullOrEmpty(requestHash))
                {
                    SaveLastResponse(requestHash, response);
                }
                // Parse the response
                Dictionary<string, ProductInfo> prices = ProcessResponse(settings, response, logger, context);

                if (prices != null && prices.Count > 0)
                {
                    if (updateCache)
                    {
                        Dictionary<string, ProductInfo> cachedProductInfos = ResponseCache.GetProductInfos(productCacheLevel, context.User);
                        if (cachedProductInfos != null)
                        {
                            // Cache prices
                            foreach (string productKey in prices.Keys)
                            {
                                if (cachedProductInfos.ContainsKey(productKey))
                                {
                                    cachedProductInfos.Remove(productKey);
                                }
                                cachedProductInfos.Add(productKey, prices[productKey]);
                            }
                        }
                    }
                }
                else
                {
                    result = false;
                    if (httpStatusCode == HttpStatusCode.InternalServerError && productCacheLevel == ResponseCacheLevel.Page && !string.IsNullOrEmpty(requestHash))
                    {
                        SaveLastResponse(requestHash, new XmlDocument());
                    }
                }
            }
            else
            {
                // error occurred
                result = false;
                if (httpStatusCode == HttpStatusCode.InternalServerError && productCacheLevel == ResponseCacheLevel.Page && !string.IsNullOrEmpty(requestHash))
                {
                    SaveLastResponse(requestHash, new XmlDocument());
                }
            }
            Diagnostics.ExecutionTable.Current.Add("DynamicwebLiveIntegration.ProductManager.FetchProductInfos END");
            return result;
        }

        /// <summary>
        /// Builds the product request.
        /// </summary>
        /// <param name="products">The products.</param>
        /// <returns>System.String.</returns>
        private static string BuildProductRequest(Settings settings, Dictionary<Product, double> products, LiveContext context, Logger logger)
        {
            var xmlGenerator = new ProductInfoXmlGenerator();
            var xmlGeneratorSettings = new ProductInfoXmlGeneratorSettings
            {
                AddProductFieldsToRequest = settings.AddProductFieldsToRequest,
                GetUnitPrices = settings.UseUnitPrices,
                LiveIntegrationSubmitType = SubmitType.Live,
                ReferenceName = "ProductInfoLive",
                Context = context
            };

            var xml = xmlGenerator.GenerateProductInfoXml(
                settings,
                products,
                xmlGeneratorSettings, logger);

            return xml;
        }


        private static Dictionary<Product, double> GetProductsForRequest(Settings settings, ResponseCacheLevel productCacheLevel, Dictionary<Product, double> products, Logger logger, LiveContext context, bool doCurrencyCheck)
        {
            // Check for existence of the given products in the Cache
            Dictionary<Product, double> productsForRequest = new Dictionary<Product, double>();
            bool getProductInformationForAllVariants = settings.GetProductInformationForAllVariants;

            foreach (var productWithQuantity in products)
            {
                Product product = productWithQuantity.Key;
                string productIdentifier = ProductProvider.GetProductIdentifier(settings, product);
                bool isProductCached = ResponseCache.IsProductInCache(productCacheLevel, productIdentifier, context.User,
                    doCurrencyCheck ? context.Currency : null);

                if (!isProductCached)
                {
                    // setting to request all variants in request
                    if (getProductInformationForAllVariants)
                    {
                        var variants = Services.VariantCombinations.GetVariantCombinations(product.Id)?.Select(vc => Services.Products.GetProductById(vc.ProductId, vc.VariantId, false)).ToList();
                        if (variants != null)
                        {
                            variants = GetFilteredVariants(variants);
                            foreach (var variant in variants)
                            {
                                var variantProductIdentifier = ProductProvider.GetProductIdentifier(settings, variant);
                                var isVariantProductCached = ResponseCache.IsProductInCache(productCacheLevel, variantProductIdentifier, context.User,
                                    doCurrencyCheck ? context.Currency : null);

                                if (!isVariantProductCached)
                                {
                                    var isVariantProductAdded = productsForRequest.ContainsKey(variant);
                                    var isVariantProductLivePriceEnabled = ProductProvider.IsLivePriceEnabledForProduct(variant);

                                    if (!isVariantProductAdded && isVariantProductLivePriceEnabled && variant.HasIdentifier(settings))
                                    {
                                        productsForRequest.Add(variant, productWithQuantity.Value);
                                    }
                                }
                            }
                        }
                    }

                    product = ProductProvider.GetProductFromVariantComboId(product, logger);

                    if (string.IsNullOrEmpty(product.VariantId) || GetFilteredVariants(new List<Product>() { product }).Any())
                    {
                        productIdentifier = ProductProvider.GetProductIdentifier(settings, product);
                        isProductCached = ResponseCache.IsProductInCache(productCacheLevel, productIdentifier, context.User,
                            doCurrencyCheck ? context.Currency : null);
                        if (!isProductCached)
                        {
                            bool isProductAdded = productsForRequest.ContainsKey(product);
                            bool isProductLivePriceEnabled = ProductProvider.IsLivePriceEnabledForProduct(product);
                            if (!isProductAdded && isProductLivePriceEnabled && product.HasIdentifier(settings))
                            {
                                productsForRequest.Add(product, productWithQuantity.Value);
                            }
                        }
                    }
                }
            }

            return productsForRequest;
        }

        private static List<Product> GetFilteredVariants(List<Product> variants)
        {
            bool hideInactiveVariants = SystemConfiguration.Instance.GetBoolean("/Globalsettings/Ecom/Product/DontAllowLinksToVariantIfNotActive");
            bool hideInactiveProducts = SystemConfiguration.Instance.GetBoolean("/Globalsettings/Ecom/Product/DontAllowLinksToProductIfNotActive");
            if (hideInactiveVariants || hideInactiveProducts)
            {
                bool inactiveVariantsNotSet = string.IsNullOrEmpty(SystemConfiguration.Instance.GetValue("/Globalsettings/Ecom/Product/DontAllowLinksToVariantIfNotActive"));
                List<Product> result = new List<Product>();
                foreach (var variant in variants)
                {
                    if (!variant.Active && (hideInactiveVariants || inactiveVariantsNotSet && hideInactiveProducts))
                    {
                        continue;
                    }
                    result.Add(variant);
                }
                return result;
            }
            else
            {
                return variants;
            }
        }

        /// <summary>
        /// Processes the response with product info.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns>Dictionary&lt;System.String, ProductInfo&gt;.</returns>
        private static Dictionary<string, ProductInfo> ProcessResponse(Settings settings, XmlDocument response, Logger logger, LiveContext context)
        {
            if (response == null)
            {
                return null;
            }

            try
            {
                Dictionary<string, ProductInfo> infos = new Dictionary<string, ProductInfo>();
                string priceLookupKey(string productId, string variantId) => string.Join("|", productId, variantId);
                ILookup<string, ProductPrice> pricesLookup = ProductProvider.ExtractPrices(settings, response, logger).ToLookup(p => priceLookupKey(p.ProductId, p.ProductVariantId), StringComparer.OrdinalIgnoreCase);

                var items = response.SelectNodes("//item [@table='EcomProducts']");
                if (items != null)
                {
                    foreach (XmlNode item in items)
                    {
                        string productId = item.SelectSingleNode("column [@columnName='ProductId']")?.InnerText ?? string.Empty;
                        string variantId = item.SelectSingleNode("column [@columnName='ProductVariantId']")?.InnerText ?? string.Empty;
                        string productIdentifier = item.SelectSingleNode("column [@columnName='ProductIdentifier']")?.InnerText ?? string.Empty;
                        string productPrice = item.SelectSingleNode("column [@columnName='ProductPrice']")?.InnerText ?? string.Empty;
                        string productPriceWithVat = item.SelectSingleNode("column [@columnName='ProductPriceWithVat']")?.InnerText ?? string.Empty;

                        ProductInfo productInfo = new ProductInfo
                        {

                            // Set ProductId
                            ["ProductId"] = productIdentifier,

                            // Set price
                            ["TotalPrice"] = !string.IsNullOrEmpty(productPrice) ? Helpers.ToDouble(settings, logger, productPrice) : (double?)null,
                            ["TotalPriceWithVat"] = !string.IsNullOrEmpty(productPriceWithVat) ? Helpers.ToDouble(settings, logger, productPriceWithVat) : (double?)null,
                            ["CurrencyCode"] = context?.Currency?.Code
                        };

                        // Set stock
                        var stock = item.SelectSingleNode("column [@columnName='ProductStock']")?.InnerText;
                        if (stock != null)
                        {
                            productInfo["Stock"] = Helpers.ToDouble(settings, logger, stock);
                        }
                        else
                        {
                            productInfo["Stock"] = null;
                        }

                        // Set prices
                        productInfo["Prices"] = pricesLookup[priceLookupKey(productId, variantId)].ToList();

                        // Set ProductCustomFields
                        if (settings.AddProductFieldsToRequest)
                        {
                            foreach (ProductField pf in ProductField.GetProductFields())
                            {
                                var fieldNode = item.SelectSingleNode($"column [@columnName='{pf.SystemName}']");
                                if (fieldNode != null)
                                {
                                    productInfo[pf.SystemName] = fieldNode.InnerText;
                                }
                            }
                        }

                        if (settings.UseUnitPrices)
                        {
                            productInfo["ProductDefaultUnitId"] = item.SelectSingleNode("column [@columnName='ProductDefaultUnitId']")?.InnerText;
                        }

                        // avoid exception to duplicate products in XML
                        if (!infos.ContainsKey(productIdentifier))
                        {
                            infos.Add(productIdentifier, productInfo);
                        }
                    }
                }

                return infos;
            }
            catch (Exception e)
            {
                logger.Log(ErrorLevel.Error, $"Response does not match schema: '{e.Message}'.");
            }

            return null;
        }

        /// <summary>
        /// Saves the last response to cache for 30 seconds.
        /// </summary>
        private static void SaveLastResponse(string requestHash, XmlDocument response)
        {
            Caching.Cache.Current.Set(requestHash, response, new CacheItemPolicy() { AbsoluteExpiration = DateTime.Now.AddSeconds(30) });
        }

        /// <summary>
        /// Gets the ProductInfo from the ERP
        /// </summary>
        /// <param name="product">The product</param>
        /// <returns></returns>
        public static ProductInfo GetProductInfo(Product product, Settings settings, User user)
        {
            return GetProductInfo(product, settings, user, null);
        }

        /// <summary>
        /// Gets the ProductInfo from the ERP
        /// </summary>
        /// <param name="product">The product</param>
        /// <returns></returns>
        public static ProductInfo GetProductInfo(Product product, Settings settings, User user, LiveContext context)
        {
            ProductInfo productInfo = null;
            if (product != null)
            {
                string productIdentifier = ProductProvider.GetProductIdentifier(settings, product);
                var productCacheLevel = settings.GetProductCacheLevel();
                if (ResponseCache.IsProductInCache(productCacheLevel, productIdentifier, user, context?.Currency))
                {
                    Dictionary<string, ProductInfo> productInfoCache = ResponseCache.GetProductInfos(productCacheLevel, user);
                    productInfoCache.TryGetValue(productIdentifier, out productInfo);
                }
            }
            return productInfo;
        }
    }
}