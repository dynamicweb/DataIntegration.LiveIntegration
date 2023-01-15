
namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators.IntegrationCustomerCenter
{
    /// <summary>
    /// A settings class to drive the generation of integration customer center orders list XML.
    /// </summary>
    /// <seealso cref="XmlGeneratorSettings" />
    public class ItemListXmlGeneratorSettings : XmlGeneratorSettings
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
        /// Page size
        /// </summary>
        public int PageSize;
        /// <summary>
        /// Page index
        /// </summary>
        public int PageIndex;
    }
}
