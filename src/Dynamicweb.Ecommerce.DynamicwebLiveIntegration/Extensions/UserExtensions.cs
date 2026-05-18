using Dynamicweb.Security.UserManagement;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions
{
    /// <summary>
    /// Provides extension methods for the User type to determine whether live integration prices or discounts are
    /// disabled for a user, including evaluation of ancestor group settings.
    /// </summary>
    /// <remarks>These extension methods evaluate both the user's own settings and those of any ancestor
    /// groups to determine if live integration features are disabled. Results are cached per user within the current
    /// context to improve performance when called repeatedly. Use these methods to check feature availability before
    /// performing operations that depend on live integration prices or discounts.</remarks>
    public static class UserExtensions
    {
        private const string PricesDisabledCacheKeyPrefix = "DynamicwebLiveIntegrationIsLivePricesDisabled";
        private const string DiscountsDisabledCacheKeyPrefix = "DynamicwebLiveIntegrationIsLiveDiscountsDisabled";

        /// <summary>
        /// Determines whether live integration prices are disabled for the specified user, considering user and
        /// ancestor group settings.
        /// </summary>
        /// <remarks>This method checks the user's direct setting and, if necessary, evaluates ancestor
        /// group flags. Results may be cached for performance.</remarks>
        /// <param name="user">The user for whom to check if live integration prices are disabled. Cannot be null.</param>
        /// <returns>true if live integration prices are disabled for the user or any of their ancestor groups; otherwise, false.</returns>
        public static bool IsLiveIntegrationPricesDisabled(this User user)
        {
            if (user is null)
                return false;

            if (user.IsLivePricesDisabled)
                return true;

            var key = $"{PricesDisabledCacheKeyPrefix}{user.ID}";
            if (Context.Current?.Items?[key] is bool cached)
                return cached;

            return ComputeAndCacheAncestorGroupFlags(user).pricesDisabled;
        }

        /// <summary>
        /// Determines whether live integration discounts are disabled for the specified user, considering both user and
        /// ancestor group settings.
        /// </summary>
        /// <remarks>This method checks the user's own setting and, if necessary, evaluates ancestor group
        /// flags. Results may be cached for performance.</remarks>
        /// <param name="user">The user for whom to check the live integration discounts status. Cannot be null.</param>
        /// <returns>true if live integration discounts are disabled for the user or any of their ancestor groups; otherwise,
        /// false.</returns>
        public static bool IsLiveIntegrationDiscountsDisabled(this User user)
        {
            if (user is null)
                return false;

            if (user.IsLiveDiscountsDisabled)
                return true;

            var key = $"{DiscountsDisabledCacheKeyPrefix}{user.ID}";
            if (Context.Current?.Items?[key] is bool cached)
                return cached;

            return ComputeAndCacheAncestorGroupFlags(user).discountsDisabled;
        }

        // Iterates ancestor groups once to populate both cache entries, short-circuiting when both flags are found.
        private static (bool pricesDisabled, bool discountsDisabled) ComputeAndCacheAncestorGroupFlags(User user)
        {
            bool pricesDisabled = false;
            bool discountsDisabled = false;
            foreach (var group in user.GetAncestorGroups())
            {
                pricesDisabled |= group.IsLivePricesDisabled;
                discountsDisabled |= group.IsLiveDiscountsDisabled;
                if (pricesDisabled && discountsDisabled)
                    break;
            }
            if (Context.Current?.Items is not null)
            {
                Context.Current.Items[$"{PricesDisabledCacheKeyPrefix}{user.ID}"] = pricesDisabled;
                Context.Current.Items[$"{DiscountsDisabledCacheKeyPrefix}{user.ID}"] = discountsDisabled;
            }
            return (pricesDisabled, discountsDisabled);
        }
    }
}
