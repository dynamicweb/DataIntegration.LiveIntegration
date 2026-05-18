using Dynamicweb.Core;
using Dynamicweb.Security.UserManagement;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions
{
    internal static class UserExtensions
    {
        internal static bool IsLiveIntegrationPricesDisabled(this User user)
        {
            if (user.IsLivePricesDisabled)
                return true;

            var key = $"DynamicwebLiveIntegrationIsLivePricesDisabled{user.ID}";
            if (Context.Current?.Items?[key] is { } cached)
                return Converter.ToBoolean(cached);

            return ComputeAndCacheAncestorGroupFlags(user).pricesDisabled;
        }

        internal static bool IsLiveIntegrationDiscountsDisabled(this User user)
        {
            if (user.IsLiveDiscountsDisabled)
                return true;

            var key = $"DynamicwebLiveIntegrationIsLiveDiscountsDisabled{user.ID}";
            if (Context.Current?.Items?[key] is { } cached)
                return Converter.ToBoolean(cached);

            return ComputeAndCacheAncestorGroupFlags(user).discountsDisabled;
        }

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
                Context.Current.Items[$"DynamicwebLiveIntegrationIsLivePricesDisabled{user.ID}"] = pricesDisabled;
                Context.Current.Items[$"DynamicwebLiveIntegrationIsLiveDiscountsDisabled{user.ID}"] = discountsDisabled;
            }
            return (pricesDisabled, discountsDisabled);
        }
    }
}
