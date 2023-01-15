using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using System;
using System.Globalization;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions
{    
    /// <summary>
    /// Extensions methods for formatting of dates and numbers.
    /// </summary>
    internal static class IntegrationExtensions
    {        
        /// <summary>
        /// Rounds the value to the specified number of decimals and then formats it using <see cref="GetNumberFormatter" />.
        /// </summary>
        /// <param name="value">The value to process.</param>
        /// <param name="numberOfDecimals">The number of decimals.</param>
        /// <returns>System.String.</returns>
        internal static string ToIntegrationString(this double value, Settings settings, Logger logger, int numberOfDecimals = 2)
        {
            return Math.Round(value, numberOfDecimals).ToString(GetNumberFormatter(settings, logger));
        }

        /// <summary>
        /// Formats the date time to a sortable date/time format.
        /// </summary>
        /// <param name="value">The value to process.</param>        
        /// <returns>System.String.</returns>
        internal static string ToIntegrationString(this DateTime value)
        {
            return value.ToString("s");
        }

        internal static string ToIntegrationString(this object obj, Settings settings, Logger logger)
        {
            string value;
            if (obj is double)
            {
                value = ((double)obj).ToIntegrationString(settings, logger);
            }
            else if (obj is DateTime)
            {
                value = ((DateTime)obj).ToIntegrationString();
            }
            else
            {
                value = obj.ToString();
            }
            return value;
        }

        /// <summary>
        /// Gets the number formatter.
        /// </summary>
        /// <returns>NumberFormatInfo.</returns>
        private static NumberFormatInfo GetNumberFormatter(Settings settings, Logger logger)
        {
            CultureInfo culture = Helpers.GetCultureInfo(settings, logger);
            return culture != null ? culture.NumberFormat : NumberFormatInfo.InvariantInfo;
        }        
    }
}