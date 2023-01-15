using System;
using System.Linq;
using Dynamicweb.Content.Files.Information;
using Dynamicweb.DataIntegration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using Dynamicweb.Ecommerce.Orders;
using Dynamicweb.Extensibility.AddIns;
using Dynamicweb.Extensibility.Editors;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.ScheduledTasks
{
    public abstract class BatchIntegrationScheduledTaskAddinBase : BatchIntegrationScheduledTaskAddin
    {
        public DateTime LastSuccessfulRun { get; set; } = DateTime.Now.Date;

        [AddInParameter("First notification after 'x' minutes")]
        [AddInParameterEditor(typeof(IntegerNumberParameterEditor), "allowNegativeValues=false;minValue=5;NewGUI=true;none=false;")]
        public int MinutesForFirstNotification { get; set; }

        [AddInParameter("Notify every 'x' minutes")]
        [AddInParameterEditor(typeof(IntegerNumberParameterEditor), "allowNegativeValues=false;minValue=5;NewGUI=true;none=false;")]
        public int MinutesForNotification { get; set; }

        protected void SetSuccessfulRun()
        {
            LastSuccessfulRun = DateTime.Now;
        }

        protected void SendMailWithFrequency(string message, Settings settings, MessageType messageType = MessageType.Success)
        {
            var frequencySettings = settings?.NotificationSendingFrequency;
            if (!string.IsNullOrEmpty(frequencySettings))
            {
                var frequency = Helpers.GetEnumValueFromString(frequencySettings, NotificationFrequency.Never);
                if (frequency != NotificationFrequency.Never)
                {
                    if (IsTimeForNotification(MinutesForFirstNotification) || IsTimeForNotification(MinutesForNotification))
                    {
                        SendMail(message, messageType);
                    }
                }
            }
        }

        private bool IsTimeForNotification(int interval)
        {
            const int deviation = 3;

            var ticksSinceLastRun = DateTime.Now.Ticks - LastSuccessfulRun.Ticks;
            var minutesPast = TimeSpan.FromTicks(ticksSinceLastRun).TotalMinutes;

            var minutesPastMod = minutesPast % interval;

            return minutesPastMod < deviation;
        }

        protected Settings GetSettings(string shopId = null)
        {
            Settings settings = null;
            if (!string.IsNullOrEmpty(shopId))
            {
                settings = SettingsManager.GetSettingsByShop(shopId);
            }
            if (settings == null)
            {
                settings = SettingsManager.AllSettings.Where(i => i.IsLiveIntegrationEnabled).FirstOrDefault();
            }
            return settings;
        }                
    }
}
