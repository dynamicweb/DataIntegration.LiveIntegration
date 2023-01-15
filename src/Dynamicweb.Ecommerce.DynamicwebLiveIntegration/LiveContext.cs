using Dynamicweb.Ecommerce.International;
using Dynamicweb.Ecommerce.Prices;
using Dynamicweb.Ecommerce.Shops;
using Dynamicweb.Security.UserManagement;
using System.Linq;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration
{
    public class LiveContext
    {
        public Currency Currency { get; }
        public User User { get; }

        public Country Country { get; }

        public Shop Shop { get; }

        public PriceContext PriceContext { get; set; }

        public LiveContext(Currency currency, User user, Shop shop)
        {
            Currency = currency;
            User = user;
            Shop = shop;
            Country = GetCountry(user);
        }

        public LiveContext(PriceContext priceContext)
        {
            PriceContext = priceContext;
            Shop = Services.Shops.GetShop(priceContext.Shop?.Id);
            User = priceContext.Customer;
            Currency = priceContext.Currency;
            Country = priceContext.Country;
        }

        private Country GetCountry(User user)
        {
            Country country = null;
            if (user != null && !string.IsNullOrEmpty(user.CountryCode))
            {
                country = Services.Countries.GetCountry(user.CountryCode);
            }
            if (country is null)
            {
                var countryCode = Services.Languages.GetDefaultLanguage().CountryCode;
                if (!string.IsNullOrEmpty(countryCode))
                {
                    country = Services.Countries.GetCountry(countryCode);
                }
            }
            if (country is null)
            {
                country = Services.Countries.GetCountries().FirstOrDefault();
            }
            if (country is null)
            {
                country = new Country();
            }
            return country;
        }
    }
}
