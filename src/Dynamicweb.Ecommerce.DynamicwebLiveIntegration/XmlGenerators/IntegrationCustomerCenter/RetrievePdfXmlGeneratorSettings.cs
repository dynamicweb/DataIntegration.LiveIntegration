
namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators.IntegrationCustomerCenter
{
    /// <summary>
    /// A settings class to drive the generation of integration customer center retrieve pdf XML.
    /// </summary>
    /// <seealso cref="XmlGeneratorSettings" />
    public class RetrievePdfXmlGeneratorSettings : XmlGeneratorSettings
    {
        /// <summary>
        /// Report type
        /// </summary>
        public string Type;
        /// <summary>
        /// Customer id
        /// </summary>
        public string CustomerId;
        /// <summary>
        /// Document or item id
        /// </summary>
        public string ItemId;
    }
}
