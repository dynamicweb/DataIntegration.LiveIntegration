using Dynamicweb.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration
{
    /// <summary>
    /// Settings manager
    /// </summary>
    public static class SettingsManager
    {
        private static Lazy<List<Settings>> _lazySettings = new Lazy<List<Settings>>(LoadSettings);
        private static Dictionary<string, Settings> _activeSettings;

        /// <summary>
        /// All settings
        /// </summary>
        public static List<Settings> AllSettings
        {
            get { return _lazySettings.Value; }
        }

        /// <summary>
        /// Active shop id settings dictionary
        /// </summary>
        public static Dictionary<string, Settings> ActiveSettingsByShopId
        {
            get
            {
                if (_activeSettings == null)
                {
                    Dictionary<string, Settings> result = new Dictionary<string, Settings>();
                    foreach (var settings in AllSettings.Where(i => i.IsLiveIntegrationEnabled))
                    {
                        if (settings.ShopId == null)
                        {
                            settings.ShopId = string.Empty;
                        }
                        if (!result.ContainsKey(settings.ShopId))
                        {
                            result.Add(settings.ShopId, settings);
                        }
                    }
                    _activeSettings = result;
                }
                return _activeSettings;
            }
        }

        /// <summary>
        /// Reload settings
        /// </summary>
        public static void Reload()
        {
            if (_lazySettings.IsValueCreated)
            {
                _lazySettings = new Lazy<List<Settings>>(LoadSettings);
                _activeSettings = null;
                Connectors.Connector.ClearCache();
            }
        }

        /// <summary>
        /// Gets new instance id
        /// </summary>
        /// <returns></returns>
        public static string GetNextNewInstanceId()
        {
            int maxId = 0;
            foreach (var s in AllSettings)
            {
                if (int.TryParse(s.InstanceId, out int id) && maxId < id)
                {
                    maxId = id;
                }
            }
            return (++maxId).ToString();
        }

        public static Settings GetSettingsByShop(string shopId)
        {
            if (!ActiveSettingsByShopId.TryGetValue(string.Empty, out Settings settings) && !string.IsNullOrEmpty(shopId))
            {
                ActiveSettingsByShopId.TryGetValue(shopId, out settings);
            }
            return settings;
        }

        internal static string GetCurrentInstanceId()
        {
            return Context.Current?.Request["DynamicwebLiveIntegrationInstanceId"];
        }


        private static List<Settings> LoadSettings()
        {
            List<Settings> result = new List<Settings>();
            var handler = new SettingsFileHandler();
            var folderPath = Path.GetDirectoryName(SystemInformation.MapPath($"/Files/System/LiveIntegration/"));
            if (Directory.Exists(folderPath))
            {
                foreach (string file in Directory.GetFiles(folderPath, $"{Constants.AddInName}*.Setup.xml"))
                {
                    var settings = SettingsFileHandler.LoadSettings(file);
                    handler.SetUpWatching(settings);
                    result.Add(settings);
                }
            }
            return result;
        }

        internal static Settings GetSettingsById(string instanceId)
        {
            foreach (var settings in AllSettings)
            {
                if ((string.IsNullOrEmpty(instanceId) && string.IsNullOrEmpty(settings.InstanceId))
                    || settings.InstanceId == instanceId)
                {
                    return settings;
                }
            }
            return null;
        }

        internal static Settings GetCurrentSettingsForBackend()
        {
            Settings settings = null;
            string id = GetCurrentInstanceId();
            if (!string.IsNullOrEmpty(id))
            {
                settings = GetSettingsById(id);
            }
            if (settings == null)
            {
                settings = new Settings();
            }
            return settings;
        }        
    }
}
