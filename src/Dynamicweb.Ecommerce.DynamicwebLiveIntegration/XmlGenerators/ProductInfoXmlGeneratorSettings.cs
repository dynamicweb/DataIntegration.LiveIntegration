namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators
{
    /// <summary>
    /// A settings class to drive the generation of product info XML.
    /// </summary>
    /// <seealso cref="XmlGeneratorSettings" />
    public class ProductInfoXmlGeneratorSettings : XmlGeneratorSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether [add product fields to request].
        /// </summary>
        /// <value><c>true</c> if [add product fields to request]; otherwise, <c>false</c>.</value>
        public bool AddProductFieldsToRequest { get; set; }

        /// <summary>
        /// Gets or sets if get unit prices
        /// </summary>
        /// <value><c>true</c> if [get unit prices]; otherwise, <c>false</c>.</value>
        public bool GetUnitPrices { get; set; }

        public LiveContext Context { get; set; }
    }
}