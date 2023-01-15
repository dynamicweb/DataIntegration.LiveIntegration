using System;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Licensing
{
    internal class LicenseStatus
    {
        internal string ResponseVersion { get; private set; }
        internal bool Allowed { get; private set; }

        internal bool? IsFeatureEnabled { get; private set; }

        public DateTime? LastUpdateTime { get; set; }

        public bool Expired
        {
            get
            {
                return !LastUpdateTime.HasValue || DateTime.Now.Subtract(LastUpdateTime.Value).Minutes > 5;
            }
        }

        public bool IsDifferentVersion(string version)
        {
            if (string.IsNullOrEmpty(ResponseVersion) && string.IsNullOrEmpty(version))
            {
                return false;
            }
            return !(string.Equals(ResponseVersion, version, StringComparison.OrdinalIgnoreCase));
        }

        public void UpdateVersion(string version)
        {
            ResponseVersion = version;
        }

        public void UpdateStatus(bool allowed)
        {
            Allowed = allowed;
            LastUpdateTime = DateTime.Now;
        }

        public void SetFeatureEnabled(bool enabled)
        {
            IsFeatureEnabled = enabled;
        }
    }
}
