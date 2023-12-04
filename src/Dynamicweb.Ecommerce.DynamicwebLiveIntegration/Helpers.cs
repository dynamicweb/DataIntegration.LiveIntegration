using Dynamicweb.Core;
using Dynamicweb.Ecommerce.International;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Environment;
using Dynamicweb.Security.UserManagement;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using Dynamicweb.Ecommerce.Products;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Connectors;
using Dynamicweb.Ecommerce.Orders.Discounts;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration
{
    /// <summary>
    /// Class Helpers.
    /// </summary>
    internal static class Helpers
    {
        /// <summary>
        /// Adds the child XML node to cart.
        /// </summary>
        /// <param name="xDoc">The x document.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeName">Name of the node.</param>
        /// <param name="nodeValue">The node value.</param>
        public static void AddChildXmlNodeToCart(XmlDocument xDoc, XmlElement parent, string nodeName, string nodeValue)
        {
            XmlElement node = xDoc.CreateElement("column");
            node.SetAttribute("columnName", nodeName);
            node.InnerText = nodeValue;
            parent.AppendChild(node);
        }

        /// <summary>
        /// Adds the child XML node to product list.
        /// </summary>
        /// <param name="xDoc">The x document.</param>
        /// <param name="parent">The parent.</param>
        /// <param name="nodeName">Name of the node.</param>
        /// <param name="nodeValue">The node value.</param>
        public static void AddChildXmlNodeToProductList(XmlDocument xDoc, XmlElement parent, string nodeName, string nodeValue)
        {
            XmlElement node = xDoc.CreateElement(nodeName);
            node.InnerText = nodeValue;
            parent.AppendChild(node);
        }        

        /// <summary>
        /// Returns the relevant Enum Value. If input can be parsed to Enum type, then the enum value is returned. Otherwise defaultValue is returned.
        /// </summary>
        /// <typeparam name="T">Type of Enum to parse value from</typeparam>
        /// <param name="input">To be parsed as Enum value.</param>
        /// <param name="defaultValue">Default value if input was not valid enum value.</param>
        /// <returns>Parsed or default enum value</returns>
        public static T GetEnumValueFromString<T>(string input, T defaultValue)
        {
            if (string.IsNullOrEmpty(input))
            {
                return defaultValue;
            }

            T value;
            try
            {
                value = (T)Enum.Parse(typeof(T), input, true);
            }
            catch (Exception)
            {
                return defaultValue;
            }

            return value;
        }

        /// <summary>
        /// Compares two prices.
        /// </summary>
        /// <param name="price1">The price to compare.</param>
        /// <param name="price2">The price to compare with.</param>
        /// <returns></returns>
        public static bool IsDifferentPrice(double price1, double price2)
        {
            return IsDifferent(price1, price2);
        }

        /// <summary>
        /// Compares two stock levels.
        /// </summary>
        /// <param name="stock1">The stock to compare.</param>
        /// <param name="stock2">The stock to compare with.</param>
        public static bool IsDifferentStock(double stock1, double stock2)
        {
            return IsDifferent(stock1, stock2);
        }

        /// <summary>
        /// Orders the identifier.
        /// </summary>
        /// <param name="order">The order.</param>
        /// <returns>System.String.</returns>
        public static string OrderIdentifier(Order order)
        {
            string ret = $"{order.Id}.{order.CurrencyCode}";

            foreach (OrderLine ol in order.OrderLines)
            {
                ret += $".{ol.Quantity}.{ol.ProductId}.{ol.ProductVariantId}.{ol.ProductNumber}.{ol.ProductName}.{ol.OrderLineType}";
            }

            return ret;
        }

        /// <summary>
        /// Parses the response to XML.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="responseDoc">The response document.</param>
        /// <returns><c>true</c> if response was parsed to xml, <c>false</c> otherwise.</returns>
        public static bool ParseResponseToXml(string response, Logger logger, out XmlDocument responseDoc)
        {
            bool result = true;
            responseDoc = new XmlDocument();

            try
            {
                responseDoc.LoadXml(response);
            }
            catch (Exception e)
            {
                result = false;
                logger.Log(ErrorLevel.ResponseError, $"Response is not valid XML format: '{e.Message}'.");
            }

            return result;
        }

        /// <summary>
        /// Converts the specified value to a double.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>System.Double.</returns>
        public static double ToDouble(Settings settings, Logger logger, string value)
        {
            double result;
            CultureInfo cultureInfo = GetCultureInfo(settings, logger);
            if (cultureInfo != null)
            {
                try
                {
                    result = double.Parse(value, cultureInfo);
                }
                catch (FormatException ex)
                {
                    logger.Log(ErrorLevel.Error, $"Error parse '{value}' to double in '{cultureInfo.Name}' culture: '{ex.Message}'.");
                    result = Converter.ToDouble(value);
                }
            }
            else
            {
                result = Converter.ToDouble(value);
            }

            return result;
        }

        /// <summary>
        /// Gets the culture information.
        /// </summary>
        /// <returns>CultureInfo.</returns>
        internal static CultureInfo GetCultureInfo(Settings settings, Logger logger)
        {
            string culture = settings.NumberFormatCulture;
            CultureInfo result;
            if (!string.IsNullOrEmpty(culture))
            {
                result = GetCultureInfo(culture, logger, true);
            }
            else
            {
                result = CultureInfo.InvariantCulture;
            }

            return result;
        }

        private static bool IsDifferent(double x, double y)
        {
            var tolerance = 0.0000001;
            var isDifferent = Math.Abs(x - y) > tolerance;
            return isDifferent;
        }

        internal static CultureInfo GetCultureInfo(string cultureInfo, Logger logger, bool logException = false)
        {
            CultureInfo result = null;

            if (!string.IsNullOrEmpty(cultureInfo))
            {
                try
                {
                    result = CultureInfo.GetCultureInfo(cultureInfo);
                }
                catch (CultureNotFoundException exception)
                {
                    if (logException)
                    {
                        logger.Log(ErrorLevel.Error, $"Culture '{cultureInfo}' not found. Message: '{exception.Message}'.");
                    }
                }
            }

            return result;
        }

        internal static string GetDiscountValue(Settings settings, Discount discount, OrderLine orderLine, Logger logger)
        {
            string discountValue = string.Empty;
            switch (discount.DiscountType)
            {
                case DiscountTypes.Amount:
                    discountValue = discount.Amount.ToIntegrationString(settings, logger);
                    break;
                case DiscountTypes.Percentage:
                    discountValue = discount.Percentage.ToIntegrationString(settings, logger);
                    break;
                case DiscountTypes.Product:
                    discountValue = orderLine.ProductId;
                    break;
                case DiscountTypes.AmountFromField:
                    discountValue = discount.AmountProductFieldName;
                    break;
                case DiscountTypes.Shipping:
                    discountValue = discount.ShippingAmount.ToIntegrationString(settings, logger) + (discount.ShippingAmount != 0 ? orderLine.Order.Currency.Code : string.Empty);
                    break;
            }
            return discountValue;
        }
        internal static void SaveTranslation(string key, string value)
        {
            Rendering.Translation.Translation.SetTranslation(key, value, ExecutingContext.GetCulture(), Rendering.Translation.KeyScope.DesignsShared, null, null);
        }

        internal static string GetTranslation(string key)
        {
            string result = null;
            if (Context.Current?.Items != null && Context.Current.Items.Contains(key))
            {
                result = (string)Context.Current.Items[key];
            }
            else
            {
                Rendering.Translation.TranslationEntryCollection translations = Rendering.Translation.Translation.GetTranslations(key,
                Rendering.Translation.KeyScope.DesignsShared, null);
                if (translations?.Values != null)
                {
                    CultureInfo culture = ExecutingContext.GetCulture(true);
                    Rendering.Translation.TranslationEntry translationEntry = translations.Values.FirstOrDefault(v => string.Equals(v?.CultureName, culture?.Name, StringComparison.OrdinalIgnoreCase));
                    if (translationEntry != null && !string.IsNullOrEmpty(translationEntry.Value))
                    {
                        result = translationEntry.Value;
                    }
                    if (Context.Current?.Items != null)
                        Context.Current.Items[key] = result;
                }
            }
            return result;
        }

        internal static User GetCurrentExtranetUser()
        {
            var user = User.GetCurrentExtranetUser();
            if (user == null && !string.IsNullOrEmpty(Context.Current?.Request?["UserId"]))
            {
                var userId = Core.Converter.ToInt32(Security.SystemTools.Crypto.Decrypt(Context.Current.Request["UserId"]));
                user = User.GetUserByID(userId);
            }
            return user;
        }        

        internal static string GetStateLabel(OrderState state)
        {
            var orderFlow = Services.OrderFlows.GetAllFlows().FirstOrDefault(of => of.ID.Equals(state.OrderFlowId));
            if (orderFlow == null)
            {
                return state.Name;
            }
            return string.Concat(state.Name, " (", orderFlow.Name, ")");
        }

        internal static string GetRequestUnitId()
        {
            return Context.Current?.Request?["UnitID"];
        }

        internal static bool CanCheckPrice(Settings settings, Product product, User user)
        {
            if (!Global.IsIntegrationActive(settings)
              || !settings.EnableLivePrices
              || Global.IsProductLazyLoad(settings)
              || (user == null && !settings.LiveProductInfoForAnonymousUsers)
              || (user != null && user.IsLivePricesDisabled)
              || !Connector.IsWebServiceConnectionAvailable(settings)
              || product == null
              || string.IsNullOrEmpty(product.Id)
              || string.IsNullOrEmpty(product.Number))
            {
                return false;
            }
            return true;
        }

        internal static Currency GetCurrentCurrency()
        {
            var currency = Common.Context.Currency;
            if (currency == null && !string.IsNullOrEmpty(Context.Current?.Request?["CurrencyId"]))
            {
                currency = Services.Currencies.GetCurrency(Context.Current?.Request?["CurrencyId"]);
            }
            return currency;
        }

        #region Session Hash helper methods

        /// <summary>
        /// Calculates the hash.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>System.String.</returns>
        internal static string CalculateHash(string request)
        {
            using (var hashEngine = new System.Security.Cryptography.HMACMD5(new byte[] { 14, 4, 78 }))
            {
                byte[] hash = hashEngine.ComputeHash(Encoding.UTF8.GetBytes(request));
                string hashString = string.Empty;

                foreach (byte x in hash)
                {
                    hashString += $"{x:x2}";
                }

                return hashString;
            }
        }

        #endregion Session Hash helper methods
    }
}