using Dynamicweb.Security.UserManagement;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions
{
    internal static class UserExtensions
    {
        private const string PricesDisabledCacheKeyPrefix = "DynamicwebLiveIntegrationIsLivePricesDisabled";
        private const string DiscountsDisabledCacheKeyPrefix = "DynamicwebLiveIntegrationIsLiveDiscountsDisabled";

        internal static bool IsLiveIntegrationPricesDisabled(this User user)
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

        internal static bool IsLiveIntegrationDiscountsDisabled(this User user)
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
