using Dynamicweb.Caching;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Products;
using Dynamicweb.Ecommerce.International;
using Dynamicweb.Security.UserManagement;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Cache
{

    /// <summary>
    /// The class ErpResponseCache represents cache service for ERP responce.
    /// </summary>
    internal class ResponseCache
    {
        private static readonly string ProductInfosKey = $"{Constants.AssemblyVersion}Dynamicweb.eCommerce.LiveIntegration.Prices.ProductInfosCached";
        private static readonly string WebOrdersKey = $"{Constants.AssemblyVersion}Dynamicweb.eCommerce.LiveIntegration.Connector.ErpOrdersCached";
        private const int WebApiCacheTimeout = 20;

        /// <summary>
        /// Gets chached product infos.
        /// </summary>
        /// <param name="cacheModel">The model of cache.</param>
        /// <returns>The product infos.</returns>
        public static Dictionary<string, ProductInfo> GetProductInfos(ResponseCacheLevel cacheModel, User user)
        {
            //If it is WebApi requests - No Session available
            if (cacheModel == ResponseCacheLevel.Session && (Context.Current == null || Context.Current.Session == null))
            {
                int userId = user != null ? user.ID : 0;
                if (userId != 0)
                {
                    string userSessionKey = GetUserSessionKey(userId.ToString());
                    if (!Caching.Cache.Current.Contains(userSessionKey))
                    {
                        Caching.Cache.Current.Set(userSessionKey, new Dictionary<string, ProductInfo>(),
                            new CacheItemPolicy() { SlidingExpiration = TimeSpan.FromMinutes(WebApiCacheTimeout) });
                    }
                    return (Dictionary<string, ProductInfo>)Caching.Cache.Current[userSessionKey];
                }
                else
                {
                    // If it is anonymous user we can not determine what external user id is sent in the ERP request
                    // So we can not cache to not break anonymous requests for the different users with different external ids
                    // Cache per one request only
                    if (Context.Current?.Items is object)
                    {
                        return GetConnectorResponsesFromItems<ProductInfo>(ProductInfosKey);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                string key = ProductInfosKey;
                if (cacheModel == ResponseCacheLevel.Session && user?.CurrentSecondaryUser != null
                    && !string.IsNullOrEmpty(user.CustomerNumber))
                {
                    key = GetUserSessionKey(user.CustomerNumber);
                }
                return GetConnectorResponses<ProductInfo>(cacheModel, key);
            }
        }

        /// <summary>
        /// Gets chached web orders.
        /// </summary>
        /// <param name="cacheModel">The model of cache.</param>
        /// <returns>The web orders.</returns>
        public static Dictionary<string, XmlDocument> GetWebOrdersConnectorResponses(ResponseCacheLevel cacheModel)
        {
            if (cacheModel == ResponseCacheLevel.Session && (Context.Current == null || Context.Current.Session == null))
            {
                if (!Caching.Cache.Current.Contains(WebOrdersKey))
                {
                    Caching.Cache.Current.Set(WebOrdersKey, new Dictionary<string, XmlDocument>(),
                        new CacheItemPolicy() { SlidingExpiration = new TimeSpan(0, WebApiCacheTimeout, 0) });
                }
                return (Dictionary<string, XmlDocument>)Caching.Cache.Current[WebOrdersKey];
            }
            else
            {
                return GetConnectorResponses<XmlDocument>(cacheModel, WebOrdersKey);
            }
        }

        /// <summary>
        /// Gets chached web orders.
        /// </summary>
        /// <param name="cacheModel">The model of cache.</param>
        /// <returns>The web orders.</returns>
        public static Dictionary<string, T> GetConnectorResponses<T>(ResponseCacheLevel cacheModel, string cacheKey)
        {
            if (Context.Current is object)
            {
                if (cacheModel == ResponseCacheLevel.Session && Context.Current.Session is object)
                {
                    if (Context.Current.Session[cacheKey] is null)
                    {
                        Context.Current.Session.Add(cacheKey, new Dictionary<string, T>());
                    }
                    return (Dictionary<string, T>)Context.Current.Session[cacheKey];
                }
                else if (Context.Current.Items is object)
                {
                    return GetConnectorResponsesFromItems<T>(cacheKey);
                }
            }
            return null;
        }

        private static Dictionary<string, T> GetConnectorResponsesFromItems<T>(string cacheKey)
        {
            if (!Context.Current.Items.Contains(cacheKey))
            {
                Context.Current.Items.Add(cacheKey, new Dictionary<string, T>());
            }
            return (Dictionary<string, T>)Context.Current.Items[cacheKey];
        }

        /// <summary>
        /// Clears all the cache.
        /// </summary>
        public static void ClearAllCaches()
        {
            // Clear ProductInfo cache
            try
            {
                Context.Current?.Session?.Remove(ProductInfosKey);
                List<string> keysToRemove = new List<string>();
                foreach (string key in Context.Current.Session.Keys)
                {
                    if (key.StartsWith(ProductInfosKey))
                        keysToRemove.Add(key);
                }
                foreach (string key in keysToRemove)
                {
                    Context.Current?.Session?.Remove(key);
                }
            }
            catch
            {
            }

            try
            {
                Context.Current?.Items?.Remove(ProductInfosKey);
            }
            catch
            {
            }

            // Clear Responses cache
            try
            {
                Context.Current?.Session?.Remove(WebOrdersKey);
            }
            catch
            {
            }

            try
            {
                Context.Current?.Items?.Remove(WebOrdersKey);
            }
            catch
            {
            }
            if (Caching.Cache.Current != null && (Context.Current == null || Context.Current.Session == null))
            {
                Caching.Cache.Current.Remove(WebOrdersKey);
                var user = User.GetCurrentExtranetUser();
                if (user is object)
                {
                    Caching.Cache.Current.Remove(GetUserSessionKey(user.ID.ToString()));
                }
            }
        }

        /// <summary>
        /// Check if product is cached.
        /// </summary>
        /// <returns><c>True</c> if product is cached, otherwise <c>false</c>.</returns>
        public static bool IsProductInCache(ResponseCacheLevel productCacheLevel, string productIdentifier, User user)
        {
            return IsProductInCache(productCacheLevel, productIdentifier, user, null);
        }

        /// <summary>
        /// Check if product is cached.
        /// </summary>
        /// <returns><c>True</c> if product is cached, otherwise <c>false</c>.</returns>
        public static bool IsProductInCache(ResponseCacheLevel productCacheLevel, string productIdentifier, User user, Currency currency)
        {
            // Verify cache
            var cachedProductInfo = GetProductInfos(productCacheLevel, user);
            if (cachedProductInfo is object && cachedProductInfo.Keys.Count > 0)
            {
                if (currency is null)
                    return cachedProductInfo.ContainsKey(productIdentifier);

                if (cachedProductInfo.TryGetValue(productIdentifier, out var productInfo))
                {
                    return Equals(currency.Code, productInfo["CurrencyCode"]);
                }
            }

            return false;
        }

        private static string GetUserSessionKey(string userId) => $"{ProductInfosKey}{userId}";
    }
}