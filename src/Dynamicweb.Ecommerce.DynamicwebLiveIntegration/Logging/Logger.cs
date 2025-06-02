using Dynamicweb.Core;
using Dynamicweb.Core.Helpers;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration;
using Dynamicweb.Mailing;
using Dynamicweb.Rendering;
using System;
using System.Collections.Concurrent;
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
        private static readonly string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.ffff";

        /// <summary>
        /// The log file
        /// </summary>
        private readonly string _logFile;

        private readonly Settings _settings;
        private readonly TimeSpan _waitTimeout;
        private readonly NotificationFrequency _notificationFrequency;
        private static readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _lastLogMessages = new();
        private readonly int _lastLogMessagesLimit = 100;

        /// <summary>
        /// Prevents a default instance of the <see cref="Logger"/> class from being created.
        /// </summary>
        public Logger(Settings settings)
        {
            _settings = settings;

            _logFile = SystemInformation.MapPath($"/Files/System/Log/LiveIntegration/{settings.InstanceName}.log");
            _waitTimeout = TimeSpan.FromSeconds(settings.AutoPingInterval < Constants.MinPingInterval ? Constants.MinPingInterval : settings.AutoPingInterval);
            _notificationFrequency = Helpers.GetEnumValueFromString(_settings.NotificationSendingFrequency, NotificationFrequency.Never);
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
        /// Gets the error messages sine the last email was sent.
        /// </summary>
        /// <returns>Log data</returns>        
        public string GetLastLogData()
        {
            string result = string.Empty;

            if (_lastLogMessages.TryGetValue(_logFile, out var queue))
            {
                var lines = queue.ToList();
                foreach (var line in lines)
                {
                    result += line + "<br>";
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
            bool isAllowedAddToLog = IsAllowedAddToLog(errorLevel);
            if (isAllowedAddToLog)
            {
                logline = $"{DateTime.Now.ToString(DateTimeFormat)}: {errorLevel}: {logline}{System.Environment.NewLine}";

                TryAddLogLineToQueue(errorLevel, logline);

                using (var mutex = new Mutex(false, _logFile.Replace("\\", "")))
                {
                    var hasHandle = false;
                    try
                    {
                        hasHandle = mutex.WaitOne(_waitTimeout, false);
                        KeepOrTruncateFile();                        
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
                    catch (Exception)
                    {
                        throw;
                    }
                    finally
                    {
                        if (hasHandle)
                            mutex.ReleaseMutex();
                    }
                }
            }

            if (errorLevel != ErrorLevel.DebugInfo && errorLevel != ErrorLevel.EmailSend)
            {
                SendLog(logline, isAllowedAddToLog);
            }
        }

        private void TryAddLogLineToQueue(ErrorLevel errorLevel, string logline)
        {
            if (errorLevel != ErrorLevel.DebugInfo && errorLevel != ErrorLevel.EmailSend && _notificationFrequency != NotificationFrequency.Never)
            {
                _lastLogMessages.AddOrUpdate(_logFile,
                    new ConcurrentQueue<string>([logline]),
                (k, q) =>
                {
                    q.Enqueue(logline);
                    if (q.Count > _lastLogMessagesLimit)
                    {
                        q.TryDequeue(out _);
                    }
                    return q;
                });
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
            if (_notificationFrequency == NotificationFrequency.Never)
            {
                return;
            }

            // Get last time when the email was sent
            DateTime lastTimeSend = Settings.LastNotificationEmailSent;
            bool emailSent = false;
            if (lastTimeSend == DateTime.MinValue)
            {
                // used for getting the last errors appeared for the future email
                Settings.LastNotificationEmailSent = DateTime.Now;
                emailSent = SendMail(lastError, false);
            }
            else
            {
                // send email if the frequency interval already passed
                if (DateTime.Now.Subtract(lastTimeSend) >= TimeSpan.FromMinutes((double)_notificationFrequency))
                {
                    Settings.LastNotificationEmailSent = DateTime.Now;
                    if (!isLastErrorInLog)
                    {
                        emailSent = SendMail(lastError, true);
                    }
                    else
                    {
                        emailSent = SendMail(null, true);
                    }
                }
            }

            if (emailSent)
            {
                Log(ErrorLevel.EmailSend, "Send e-mail with errors");
                _lastLogMessages.TryRemove(_logFile, out _);
            }
            else
            {
                Settings.LastNotificationEmailSent = lastTimeSend;
            }
        }

        [Obsolete("Use SendMail(string message, bool getLastLogData) instead")]
        public bool SendMail(string message)
        {
            return SendMail(message, true);
        }

        /// <summary>
        /// Sends an mail with error information according to configuration.
        /// </summary>
        /// <param name="message">The error/success message to send.</param>
        /// <returns><c>true</c> if email was sent, <c>false</c> otherwise.</returns>
        public bool SendMail(string message, bool getLastLogData)
        {
            string notificationTemplate = _settings.NotificationTemplate;
            if (string.IsNullOrEmpty(notificationTemplate))
                return false;

            var recipients = _settings.NotificationRecipients;
            if (recipients is null || recipients.Count == 0)
                return false;

            Template templateInstance = new($"/DataIntegration/Notifications/{notificationTemplate}");
            templateInstance.SetTag("Ecom:LiveIntegration.AddInName", Constants.AddInName);
            bool logMsgTagExists = templateInstance.TagExists("Ecom:LiveIntegration.ErrorMessage");
            if (logMsgTagExists)
            {
                if (getLastLogData)
                {
                    message = GetLastLogData() + message;
                }
                templateInstance.SetTag("Ecom:LiveIntegration.ErrorMessage", message);
            }

            string notificationEmailSubject = _settings.NotificationEmailSubject;
            string notificationEmailSenderEmail = _settings.NotificationEmailSenderEmail;
            if (!StringHelper.IsValidEmailAddress(notificationEmailSenderEmail))
            {
                notificationEmailSenderEmail = EmailHandler.SystemMailFromAddress();
            }
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

        internal static void ClearLogMessages(Settings settings)
        {
            var logFile = SystemInformation.MapPath($"/Files/System/Log/LiveIntegration/{settings.InstanceName}.log");
            _lastLogMessages.TryRemove(logFile, out _);
        }
    }
}