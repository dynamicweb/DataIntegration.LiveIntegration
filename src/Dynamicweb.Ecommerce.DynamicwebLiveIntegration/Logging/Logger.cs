using Dynamicweb.Core;
using Dynamicweb.Core.Helpers;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Mailing;
using Dynamicweb.Rendering;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using Constants = Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration.Constants;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging
{
    /// <summary>
    /// Helper class for logging.
    /// </summary>
    public class Logger
    {
        private static readonly string DateTimeFormat = "MM/dd/yyyy hh:mm:ss.fff tt";

        /// <summary>
        /// The synchronize lock.
        /// </summary>
        private static readonly object SyncLock = new();

        /// <summary>
        /// The log file
        /// </summary>
        private readonly string _logFile;

        private readonly Settings _settings;

        /// <summary>
        /// Prevents a default instance of the <see cref="Logger"/> class from being created.
        /// </summary>
        public Logger(Settings settings)
        {
            _settings = settings;
            if (settings != null)
            {
                if (string.IsNullOrEmpty(_logFile))
                {
                    _logFile = SystemInformation.MapPath($"/Files/System/Log/LiveIntegration/{settings.InstanceName}.log");
                }
            }
        }

        /// <summary>
        /// Gets or sets a value that determines if connection errors should be logged.
        /// </summary>
        /// <value><c>true</c> if [log connection errors]; otherwise, <c>false</c>.</value>
        private bool LogConnectionErrors => _settings.LogConnectionErrors;

        /// <summary>
        /// Gets or sets a value that determines if debug info should be logged.
        /// </summary>
        /// <value><c>true</c> if [log debug information]; otherwise, <c>false</c>.</value>
        private bool LogDebugInfo => _settings.LogDebugInfo;

        /// <summary>
        /// Gets or sets a value that determines if general info should be logged.
        /// </summary>
        /// <value><c>true</c> if [log general errors]; otherwise, <c>false</c>.</value>
        private bool LogGeneralErrors => _settings.LogGeneralErrors;

        /// <summary>
        /// Gets or sets a value that determines if response errors should be logged.
        /// </summary>
        /// <value><c>true</c> if [log response errors]; otherwise, <c>false</c>.</value>
        private bool LogResponseErrors => _settings.LogResponseErrors;

        /// <summary>
        /// Sends an mail with error information according to configuration.
        /// </summary>
        /// <param name="message">The error/success message to send.</param>
        /// <returns><c>true</c> if email was sent, <c>false</c> otherwise.</returns>
        public bool SendMail(string message)
        {
            string notificationTemplate = _settings.NotificationTemplate;
            if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(notificationTemplate))
                return false;

            var recipients = _settings.NotificationRecipients;
            if (recipients is null || !recipients.Any())
                return false;

            Template templateInstance = new($"/DataIntegration/Notifications/{notificationTemplate}");
            templateInstance.SetTag("Ecom:LiveIntegration.AddInName", Constants.AddInName);
            templateInstance.SetTag("Ecom:LiveIntegration.ErrorMessage", message);

            string notificationEmailSubject = _settings.NotificationEmailSubject;
            string notificationEmailSenderEmail = _settings.NotificationEmailSenderEmail;
            string notificationEmailSenderName = _settings.NotificationEmailSenderName;

            using var mail = new System.Net.Mail.MailMessage();
            mail.IsBodyHtml = true;
            mail.Subject = notificationEmailSubject;
            mail.SubjectEncoding = System.Text.Encoding.UTF8;
            mail.From = new(notificationEmailSenderEmail, notificationEmailSenderName, System.Text.Encoding.UTF8);

            // Set parameters for MailMessage
            foreach (var email in recipients)
                mail.To.Add(email);
            mail.BodyEncoding = System.Text.Encoding.UTF8;

            // Render Template and set as Body
            mail.Body = templateInstance.Output();

            // Send mail
            return EmailHandler.Send(mail);
        }

        /// <summary>
        /// Gets the error messages sine the last email was sent.
        /// </summary>
        /// <returns>Log data</returns>
        public string GetLastLogData()
        {
            string result = string.Empty;

            lock (SyncLock)
            {
                foreach (var line in File.ReadLines(_logFile, System.Text.Encoding.UTF8).Reverse())
                {
                    if (line.Contains(ErrorLevel.DebugInfo.ToString()))
                    {
                        // ignore debug info
                    }
                    else if (!line.Contains(ErrorLevel.EmailSend.ToString()))
                    {
                        result += line + "<br>";
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Logs at the specified error level.
        /// </summary>
        /// <param name="errorLevel">The error level.</param>
        /// <param name="logline">The log line.</param>
        public void Log(ErrorLevel errorLevel, string logline)
        {
            if (!string.IsNullOrEmpty(_logFile))
            {
                bool isAllowedAddToLog = IsAllowedAddToLog(errorLevel);
                lock (SyncLock)
                {
                    if (isAllowedAddToLog)
                    {
                        KeepOrTruncateFile();
                        logline = $"{DateTime.Now.ToString(DateTimeFormat)}: {errorLevel}: {logline}{System.Environment.NewLine}";

                        try
                        {
                            TextFileHelper.WriteTextFile(logline, _logFile, true, System.Text.Encoding.UTF8);
                        }
                        catch (IOException)
                        {
                            Thread.Sleep(500);
                            TextFileHelper.WriteTextFile(logline, _logFile, true, System.Text.Encoding.UTF8);
                        }
                        catch
                        {
                            var fileName = SystemInformation.MapPath(
                                $"/Files/System/Log/LiveIntegration/{Guid.NewGuid()}.log");
                            TextFileHelper.WriteTextFile(logline, fileName, false, System.Text.Encoding.UTF8);
                        }
                    }
                }

                if (errorLevel != ErrorLevel.DebugInfo && errorLevel != ErrorLevel.EmailSend)
                {
                    SendLog(logline, isAllowedAddToLog);
                }
            }
        }

        /// <summary>
        /// Determines whether the configuration is set to log at the specified log level.
        /// </summary>
        /// <param name="level">The level to check.</param>
        /// <returns><c>true</c> if [is allowed add to log]; otherwise, <c>false</c>.</returns>
        private bool IsAllowedAddToLog(ErrorLevel level)
        {
            bool result = false;
            switch (level)
            {
                case ErrorLevel.ConnectionError:
                    result = LogConnectionErrors;
                    break;

                case ErrorLevel.ResponseError:
                    result = LogResponseErrors;
                    break;

                case ErrorLevel.Error:
                    result = LogGeneralErrors;
                    break;

                case ErrorLevel.DebugInfo:
                    result = LogDebugInfo;
                    break;

                case ErrorLevel.EmailSend:
                    result = true;
                    break;
            }

            return result;
        }

        /// <summary>
        /// Rolls over the log file and creates a new one or truncates the existing file, depending on the settings.
        /// </summary>
        private void KeepOrTruncateFile()
        {
            FileInfo fi = new(_logFile);
            int maxSize = _settings.LogMaxSize;
            if (maxSize > 100)
            {
                maxSize = 100;
            }

            if (!fi.Exists || fi.Length < maxSize * 1024 * 1024)
            {
                return;
            }

            if (_settings.KeepLogFiles)
            {
                MoveToHistoryFile(fi);
            }
            else
            {
                TruncateLogFileFileInfo(fi, maxSize);
            }
        }

        /// <summary>
        /// Moves to history file.
        /// </summary>
        /// <param name="fi">The fi.</param>
        private void MoveToHistoryFile(FileInfo fi)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(_logFile);
                var newFileName = $"{fileName}-{DateTime.Now:yyyyMMddHHmmss}{fi.Extension}";
                var folder = fi.DirectoryName;

                if (!string.IsNullOrEmpty(folder))
                {
                    var newLocation = Path.Combine(folder, newFileName);
                    File.Move(_logFile, newLocation);
                }
            }
            catch
            {
                var logLine = $"{DateTime.Now.ToString(DateTimeFormat)}: {ErrorLevel.Error}: Cannot move log file.{System.Environment.NewLine}";
                TextFileHelper.WriteTextFile(logLine, _logFile, true, System.Text.Encoding.UTF8);
            }
        }

        /// <summary>
        /// Sends the log.
        /// </summary>
        /// <param name="lastError">The last error.</param>
        /// <param name="isLastErrorInLog">if set to <c>true</c> [is last error in log].</param>
        private void SendLog(string lastError, bool isLastErrorInLog)
        {
            string frequencySettings = _settings.NotificationSendingFrequency;
            if (string.IsNullOrEmpty(frequencySettings))
            {
                return;
            }

            var frequency = Helpers.GetEnumValueFromString(frequencySettings, NotificationFrequency.Never);
            if (frequency == NotificationFrequency.Never)
            {
                return;
            }

            // Get last time when the email was sent
            DateTime lastTimeSend = Settings.LastNotificationEmailSent;
            bool emailSent = false;
            if (lastTimeSend == DateTime.MinValue)
            {
                emailSent = SendMail(lastError);
            }
            else
            {
                // send email if the frequency interval already passed
                if (DateTime.Now.Subtract(lastTimeSend) >= TimeSpan.FromMinutes((double)frequency))
                {
                    if (!isLastErrorInLog)
                    {
                        emailSent = SendMail(GetLastLogData() + lastError);
                    }
                    else
                    {
                        emailSent = SendMail(GetLastLogData());
                    }
                }
            }

            if (emailSent)
            {
                Log(ErrorLevel.EmailSend, "Send e-mail with errors");

                // used for getting the last errors appeared for the future email
                Settings.LastNotificationEmailSent = DateTime.Now;
            }
        }

        /// <summary>
        /// Truncates the log file.
        /// </summary>
        /// <param name="fi">The fi.</param>
        /// <param name="maxSize">The maximum size.</param>
        private void TruncateLogFileFileInfo(FileInfo fi, int maxSize)
        {
            try
            {
                string folder = fi.DirectoryName;
                if (folder != null)
                {
                    string newFile = Path.Combine(folder, Path.GetRandomFileName());

                    using (FileStream ws = new(newFile, FileMode.CreateNew))
                    {
                        using (FileStream rs = new(_logFile, FileMode.Open))
                        {
                            rs.Seek((long)((fi.Length - maxSize) * 1024 * 1024 * 0.5), SeekOrigin.Begin); // -50%
                            int currChar;

                            while ((currChar = rs.ReadByte()) != -1)
                            {
                                if (currChar != '\r')
                                {
                                    continue;
                                }

                                currChar = rs.ReadByte();

                                if (currChar == -1 || currChar == '\n')
                                {
                                    break;
                                }
                            }

                            rs.CopyTo(ws, 1048576);
                        }

                        ws.Flush(true);
                    }

                    File.Delete(_logFile);
                    File.Move(newFile, _logFile);
                }
            }
            catch
            {
                string logLine = DateTime.Now.ToString(DateTimeFormat) + ": " + ErrorLevel.Error + ": Cannot truncate log file." + System.Environment.NewLine;
                TextFileHelper.WriteTextFile(logLine, _logFile, true, System.Text.Encoding.UTF8);
            }
        }
    }
}