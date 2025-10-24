using Dynamicweb.Core;
using Dynamicweb.Security.UserManagement;
using System.Linq;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Extensions
{
    public static class UserExtensions
    {
        internal static bool IsLiveIntegrationPricesDisabled(this User user)
        {
            if (user.IsLivePricesDisabled)
                return true;

            var key = $"DynamicwebLiveIntegrationIsLivePricesDisabled{user.ID}";
            var cacheValue = Context.Current?.Items?[key];
            if (cacheValue is not null)
            {
                return Converter.ToBoolean(cacheValue);
            }
            else
            {
                var groups = user.GetAncestorGroups();
                bool result = groups.Any(g => g.IsLivePricesDisabled);
                if (Context.Current?.Items is not null)
                {
                    Context.Current.Items[key] = result;
                }
                return result;
            }
        }
    }
}
