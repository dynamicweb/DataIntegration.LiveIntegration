using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using System;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.EndpointMonitoring
{
    /// <summary>
    /// Represents endpoint status
    /// </summary>
    internal class EndpointStatus
    {
        /// <summary>
        /// Error status
        /// </summary>
        public bool Error { get; set; }

        /// <summary>
        /// Last ERP communication time
        /// </summary>
        public DateTime? LastErpCommunication { get; set; }

        /// <summary>
        /// Checks if the status is stil valid
        /// </summary>
        /// <returns>True if status is still valid otherwise false</returns>
        public bool IsStillValid(Settings settings)
        {
            return LastErpCommunication.HasValue && 
                (DateTime.Now - LastErpCommunication.Value).TotalSeconds < 
                    (settings.AutoPingInterval < Constants.MinPingInterval ? Constants.MinPingInterval : settings.AutoPingInterval);
        }

        /// <summary>
        /// Sets endpoint communication to success.
        /// </summary>
        public void SetSuccess()
        {
            Error = false;
            LastErpCommunication = DateTime.Now;
        }

        /// <summary>
        /// Sets endpoint communication failed.
        /// </summary>
        public void SetError()
        {
            Error = true;
            LastErpCommunication = DateTime.Now;
        }
    }
}
