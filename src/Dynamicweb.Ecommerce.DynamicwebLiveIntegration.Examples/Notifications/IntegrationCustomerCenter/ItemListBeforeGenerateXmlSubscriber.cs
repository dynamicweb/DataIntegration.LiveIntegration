using Dynamicweb.Extensibility.Notifications;
using System.Linq;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Examples.Notifications.IntegrationCustomerCenter
{
    /// <summary>
    /// Class ItemListBeforeGenerateXmlSubscriber.
    /// </summary>
    /// <seealso cref="NotificationSubscriber" />
    [Subscribe(DynamicwebLiveIntegration.Notifications.IntegrationCustomerCenter.OnBeforeGenerateItemListXml)]

    public class ItemListBeforeGenerateXmlSubscriber : NotificationSubscriber
    {
        /// <summary>
        /// Call to invoke observer.
        /// </summary>
        /// <param name="notification">The notification.</param>
        /// <param name="args">The args.</param>
        public override void OnNotify(string notification, NotificationArgs args)
        {
            var myArgs = (DynamicwebLiveIntegration.Notifications.IntegrationCustomerCenter.OnBeforeGenerateItemListXmlArgs)args;

            // TODO: Add code here
            if (myArgs?.ItemListXmlGeneratorSettings != null)
            {
                var user = Security.UserManagement.User.GetCurrentExtranetUser();
                if(user != null)
                {                                     
                    Security.UserManagement.Common.CustomFields.CustomFieldValue customField = 
                        user.CustomFieldValues.FirstOrDefault(fieldValue => fieldValue.CustomField.SystemName == "AccessUser_PartnerAccountNumber");
                    if (customField != null)
                    {
                        string customerNumber = customField.Value as string;                        
                        myArgs.ItemListXmlGeneratorSettings.CustomerId = customerNumber;
                    }
                }                
            }
        }
    }
}
