namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Products
{
    /// <summary>
    /// Data class to hold information about a product's price.
    /// </summary>
    public class ProductPrice
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the product identifier.
        /// </summary>
        /// <value>The product identifier.</value>
        public string ProductId { get; set; }

        /// <summary>
        /// Gets or sets the product variant identifier.
        /// </summary>
        /// <value>The product variant identifier.</value>
        public string ProductVariantId { get; set; }

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        /// <value>The quantity.</value>
        public double? Quantity { get; set; }

        /// <summary>
        /// Gets or sets the amount.
        /// </summary>
        /// <value>The amount.</value>
        public double? Amount { get; set; } // this should have been decimal, but Dynamicweb is using doubles instead of decimals unfortunately

        /// <summary>
        /// Gets or sets the amount with VAT.
        /// </summary>
        /// <value>The amount with VAT.</value>
        public double? AmountWithVat { get; set; } // this should have been decimal, but Dynamicweb is using doubles instead of decimals unfortunately

        /// <summary>
        /// Gets or sets the user customer number.
        /// </summary>
        /// <value>The user customer number.</value>
        public string UserCustomerNumber { get; set; }

        /// <summary>
        /// Gets or sets the product unit id.
        /// </summary>
        /// <value>The product unit id.</value>
        public string UnitId { get; set; }
    }
}