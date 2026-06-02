using Dynamicweb.Core;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Updates;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Updates
{
    public sealed class LiveIntegrationUpdateProvider : UpdateProvider
    {
        public override IEnumerable<Update> GetUpdates() => new List<Update>()
        {
            new MethodUpdate("cd637b81-dd91-41ba-955e-6131c646a172", this, UpdateShippingControlMode),
        };

        private static void UpdateShippingControlMode(UpdateContext context)
        {
            if (!Directory.Exists(Path.Combine(SystemInformation.MapPath("/Files"), "System", "LiveIntegration")))
                return;

            bool updated = false;

            foreach (string file in Directory.GetFiles(Path.Combine(SystemInformation.MapPath("/Files"), "System", "LiveIntegration"), "*.Setup.xml"))
            {
                if (!file.Contains(Constants.AddInName, StringComparison.OrdinalIgnoreCase))
                    continue;


                XmlDocument doc = new XmlDocument();
                doc.LoadXml(File.ReadAllText(file));

                var shippingControlModeNode = doc.SelectSingleNode("//Settings/ShippingControlMode");
                if (shippingControlModeNode != null && !string.IsNullOrEmpty(shippingControlModeNode.InnerText))
                    continue;

                var erpControlsShippingNode = doc.SelectSingleNode("//Settings/ErpControlsShipping");
                if (string.IsNullOrEmpty(erpControlsShippingNode?.InnerText))
                    continue;

                bool erpControlsShipping = Converter.ToBoolean(erpControlsShippingNode.InnerText);

                string shippingControlMode = erpControlsShipping
                    ? Constants.ShippingControlMode.ErpControlsShipping
                    : Constants.ShippingControlMode.DynamicwebControlsShipping;

                XmlNode settingsNode = doc.SelectSingleNode("//Settings");
                if (settingsNode is not null)
                {
                    if (shippingControlModeNode != null)
                    {
                        shippingControlModeNode.InnerText = shippingControlMode;
                    }
                    else
                    {
                        XmlElement newShippingControlModeNode = doc.CreateElement("ShippingControlMode");
                        newShippingControlModeNode.InnerText = shippingControlMode;
                        settingsNode.AppendChild(newShippingControlModeNode);
                    }

                    settingsNode.RemoveChild(erpControlsShippingNode);

                    doc.Save(file);
                    updated = true;
                }
            }

            if (updated)
            {
                SettingsManager.Reload();
            }
        }
    }
}
