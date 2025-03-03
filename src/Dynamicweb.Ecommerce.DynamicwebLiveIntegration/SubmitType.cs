namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration
{
    /// <summary>
    /// Defines the way the XML is sent to the ERP.
    /// </summary>
    public enum SubmitType
    {
        /// <summary>
        /// The XML is sent as part of a live cart calculation or order create request.
        /// </summary>
        LiveOrderOrCart,

        /// <summary>
        /// The XML is sent by a scheduled task that sends orders that were previously not submitted.
        /// </summary>
        ScheduledTask,

        /// <summary>
        /// The XML is sent after a manual request from the user (through the Transfer button on the order details screen).
        /// </summary>
        ManualSubmit,

        /// <summary>
        /// The XML is created as a download from the order details screen.
        /// </summary>
        DownloadedFromBackEnd,

        /// <summary>
        /// The order XML is submitted from a template.
        /// </summary>
        FromTemplates,

        /// <summary>
        /// The XML for the user is sent as part of a live call.
        /// </summary>
        Live,

        /// <summary>
        /// The XML is sent by a Capture order scheduled task that sends orders that were previously not captured.
        /// </summary>
        CaptureTask,

        /// <summary>
        /// Request is coming from Backend
        /// </summary>
        Backend,

        /// <summary>
        /// Request is coming from the WebApi
        /// </summary>
        WebApi
    }
}