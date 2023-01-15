namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators
{
    /// <summary>
    /// A settings class to drive the generation of order XML.
    /// </summary>
    /// <seealso cref="XmlGeneratorSettings" />
    public class OrderXmlGeneratorSettings : XmlGeneratorSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether order line fields should be added to the request.
        /// </summary>
        /// <value><c>true</c> if order line fields should be added to the request; otherwise, <c>false</c>.</value>
        public bool AddOrderLineFieldsToRequest { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether order fields should be added to the request.
        /// </summary>
        /// <value><c>true</c> if order fields should be added to the request; otherwise, <c>false</c>.</value>
        public bool AddOrderFieldsToRequest { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the current request is for a cart (false) or an actual order (true).
        /// </summary>
        /// <value><c>true</c> if the order should be created in the ERP, otherwise, <c>false</c>.</value>
        public bool CreateOrder { get; set; }

        /// <summary>
        /// Gets or sets if ERP controls discount calculations
        /// </summary>
        /// <value><c>true</c> if [ERP controls discount calculations]; otherwise, <c>false</c>.</value>
        public bool ErpControlsDiscount { get; set; }

        /// <summary>
        /// Gets or sets if ERP controls shipping
        /// </summary>
        /// <value><c>true</c> if [ERP controls shipping]; otherwise, <c>false</c>.</value>
        public bool ErpControlsShipping { get; set; }
        
        /// <summary>
        /// Gets or sets the key for shipping item type.
        /// </summary>
        /// <value>The key for shipping item type.</value>
        public string ErpShippingItemType { get; set; }

        /// <summary>
        /// Gets or sets the key for shipping item.
        /// </summary>
        /// <value>The key for shipping item.</value>
        public string ErpShippingItemKey { get; set; }

        /// <summary>
        /// Gets or sets a value that determines whether to use the product number in order calculations.
        /// </summary>
        /// <value><c>true</c> if [calculate order using product number]; otherwise, <c>false</c>.</value>
        public bool CalculateOrderUsingProductNumber { get; set; }

        internal bool GenerateXmlForHash { get; set; }
    }
}