namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.XmlGenerators
{
    /// <summary>
    /// Class XmlGeneratorSettings.
    /// </summary>
    public abstract class XmlGeneratorSettings
    {
        /// <summary>
        /// Gets or sets the type of the live integration submit.
        /// </summary>
        /// <value>The type of the live integration submit.</value>
        public SubmitType LiveIntegrationSubmitType { get; set; }

        /// <summary>
        /// Gets or sets the name of the reference.
        /// </summary>
        /// <value>The name of the reference.</value>
        public string ReferenceName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the XML should be "beautified" (line breaks and indention).
        /// </summary>
        public bool Beautify { get; set; } = true;
    }
}