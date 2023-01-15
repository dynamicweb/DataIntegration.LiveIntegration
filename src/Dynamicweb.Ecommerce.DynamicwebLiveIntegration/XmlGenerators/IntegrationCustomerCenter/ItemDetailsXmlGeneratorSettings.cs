
namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators.IntegrationCustomerCenter
{    
    /// <summary>
    /// A settings class to drive the generation of integration customer center order details XML.
    /// </summary>
    /// <seealso cref="XmlGeneratorSettings" />
    public class ItemDetailsXmlGeneratorSettings : XmlGeneratorSettings
    {
        /// <summary>
        /// Order list type
        /// </summary>
        public string ItemType;
        /// <summary>
        /// Customer id
        /// </summary>
        public string CustomerId;
        /// <summary>
        /// Item id
        /// </summary>
        public string ItemId;
    }
}
