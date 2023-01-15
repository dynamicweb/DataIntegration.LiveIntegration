using Dynamicweb.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Licensing
{
    internal class LicenseService
    {
        private static readonly ConcurrentDictionary<string, LicenseStatus> EndpointCollection = new ConcurrentDictionary<string, LicenseStatus>();
        private static readonly Version D365BCMinVersion = new Version(13, 0, 0, 0);
        private static readonly Version D365BCMaxVersion = new Version(101, 0, 0, 0);
        
        private static bool IsBCFeatureEnabled
        {
            get
            {
                return Security.Licensing.LicenseManager.LicenseHasFeature("eCom_DataIntegrationERPLiveIntegration_BC");
            }
        }

        internal static bool IsLicenseValid(string endpoint)
        {
            LicenseStatus status = GetStatus(endpoint);
            if (status != null && !status.Expired)
            {
                return status.Allowed;
            }
            else
            {
                return true;
            }
        }

        private static LicenseStatus GetStatus(string endpoint)
        {
            EndpointCollection.TryGetValue(endpoint, out LicenseStatus status);
            return status;
        }

        internal static void ValidateLicense(string endpoint, string response, Logger logger)
        {
            var status = GetStatus(endpoint);
            if (status == null || status.Expired)
            {
                if (!Helpers.ParseResponseToXml(response, logger, out XmlDocument doc))
                {
                    logger.Log(ErrorLevel.DebugInfo, $"License Response is not valid XML");
                }
                else
                {
                    ValidateLicense(doc, endpoint, status, logger);
                }
            }
        }

        internal static bool ValidateLicense(string endpoint, XmlDocument doc, Logger logger)
        {
            var status = GetStatus(endpoint);
            if (status == null || status.Expired)
            {
                ValidateLicense(doc, endpoint, status, logger);
            }
            return IsLicenseValid(endpoint);
        }

        private static void ValidateLicense(XmlDocument response, string endpoint, LicenseStatus status, Logger logger)
        {
            bool updated = false;
            bool isNew = false;
            if (status == null)
            {
                status = new LicenseStatus();
                EndpointCollection.TryAdd(endpoint, status);
                isNew = true;
            }

            var versionAttribute = response.DocumentElement?.Attributes["version"];
            string versionValue = versionAttribute?.Value;
            if (isNew)
            {
                status.UpdateVersion(versionValue);
                ValidateLicense(status, versionAttribute, logger);
            }
            else
            {
                if (status.IsDifferentVersion(versionValue))
                {
                    status.UpdateVersion(versionValue);
                    ValidateLicense(status, versionAttribute, logger);
                    updated = true;
                }
                else
                {
                    if (!string.IsNullOrEmpty(versionValue))
                    {
                        //If it is the same version only check if HasFeature value was not changed
                        //If yes revalidate the license
                        if (status.IsFeatureEnabled.HasValue && status.IsFeatureEnabled.Value != IsBCFeatureEnabled)
                        {
                            ValidateLicense(status, versionAttribute, logger);
                        }
                    }
                }
            }
            if (!string.IsNullOrEmpty(versionValue) && (isNew || updated))
            {
                SaveVersions();
            }
        }

        private static void ValidateLicense(LicenseStatus status, XmlAttribute versionAttribute, Logger logger)
        {
            if (versionAttribute != null)
            {
                bool isValid = IsVersionValid(versionAttribute.Value, logger);
                bool isFeatureEnabled = true;
                if (!isValid)
                {
                    isFeatureEnabled = IsBCFeatureEnabled;
                }
                status.UpdateStatus(isValid);
                status.SetFeatureEnabled(isFeatureEnabled);
            }
            else
            {
                status.UpdateStatus(true);
            }
        }

        private static bool IsVersionValid(string version, Logger logger)
        {
            if (VersionInfo.TryParse(version, logger, out VersionInfo erpVersion))
            {
                if (!string.IsNullOrEmpty(erpVersion.ServerVersion) && VersionInfo.TryParseServerVersion(erpVersion.ServerVersion, logger, out Version navServerVersion))
                {
                    if (navServerVersion < D365BCMinVersion || navServerVersion >= D365BCMaxVersion)
                    {
                        return true;
                    }
                }
                if (IsBCFeatureEnabled)
                {
                    return true;
                }
                else
                {
                    logger.Log(ErrorLevel.Error, $"License is not valid.");
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        internal static void SaveSettings(Settings settings)
        {
            if (UrlHandler.Instance.GetWebServiceUrlsCount(settings) > 0)
            {
                SaveVersions();
            }
            else
            {
                EndpointCollection.Clear();
            }
        }

        private static void SaveVersions()
        {
            string versions = string.Join("|",
                EndpointCollection.Values.Where(s => s != null && !string.IsNullOrEmpty(s.ResponseVersion)).Select(s => s.ResponseVersion).Distinct());
            var itemsToSave = new Dictionary<string, string>
            {
                { $"{Constants.LiveIntegrationSettingsKey}/Versions", versions },
                { $"{Constants.LiveIntegrationSettingsKey}/LastVersionTime", DateTime.Now.Ticks.ToString() }
            };
            SystemConfiguration.Instance.SetValue(itemsToSave);
        }
    }
}
