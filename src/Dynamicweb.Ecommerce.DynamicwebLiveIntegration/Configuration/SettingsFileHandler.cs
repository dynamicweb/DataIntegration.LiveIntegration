using Dynamicweb.Core;
using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using System;
using System.Collections.Generic;
using System.IO;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Configuration
{
    /// <summary>
    /// Configuration file handler.
    /// </summary>
    internal class SettingsFileHandler
    {
        /// <summary>
        /// Gets the virtual configuration file path .
        /// </summary>
        private string GetSettingsFilePathPhysical(Settings settings)
        {
            return SystemInformation.MapPath($"/Files/System/LiveIntegration/{settings.InstanceName}.Setup.xml");
        }

        /// <summary>
        /// A file system watchers to watch for changes to the settings files.
        /// </summary>
        private static readonly Dictionary<string, FileSystemWatcher> Watchers = new Dictionary<string, FileSystemWatcher>();

        /// <summary>
        /// Loads the settings from disk.
        /// </summary>
        /// <exception cref="Exception"></exception>
        internal static Settings LoadSettings(string filePath)
        {
            if (Watchers.TryGetValue(filePath, out FileSystemWatcher fileSystemWatcher))
                fileSystemWatcher.EnableRaisingEvents = false;

            Settings settings = null;
            try
            {
                var xml = File.ReadAllText(filePath);
                var serializer = new SettingsSerializer();
                settings = serializer.Deserialize(xml);
                settings.SettingsFile = Path.GetFileName(filePath);
            }
            catch
            {
                string errorMessage = $"Error reading live integration file at {filePath}. Make sure the file exists, is accessible and is currently not in use.";
                throw new Exception(errorMessage);
            }
            finally
            {
                if (fileSystemWatcher != null)
                    fileSystemWatcher.EnableRaisingEvents = true;
            }

            return settings;
        }

        /// <summary>
        /// Saves the settings to disk.
        /// </summary>
        /// <param name="settings">The settings that need to be saved.</param>
        internal void SaveSettings(Settings settings)
        {
            var serializer = new SettingsSerializer();
            var xml = serializer.Serialize(settings);

            EnsureConfigurationFolderExists();
            string path = null;
            FileSystemWatcher fileSystemWatcher = null;
            try
            {
                path = GetSettingsFilePathPhysical(settings);
                settings.SettingsFile = Path.GetFileName(path);
                if(Watchers.TryGetValue(path, out fileSystemWatcher))
                {
                    fileSystemWatcher.EnableRaisingEvents = false;
                }
                File.WriteAllText(path, xml);
                SettingsManager.Reload();
            }
            catch
            {
                string errorMessage =
                    $"Error writing live integration file at {path}. Make sure the file exists, is accessible and is currently not in use.";
                var logger = GetLogger(settings);
                logger.Log(ErrorLevel.Error, errorMessage);
                throw new Exception(errorMessage);
            }
            finally
            {
                if (fileSystemWatcher != null)
                {
                    fileSystemWatcher.EnableRaisingEvents = true;
                }
            }
        }

        /// <summary>
        /// Ensures the configuration folder exists.
        /// </summary>
        internal void EnsureConfigurationFolderExists()
        {
            Directory.CreateDirectory(SystemInformation.MapPath($"/Files/System/LiveIntegration"));
        }

        /// <summary>
        /// Handles file change events and reload the settings.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="e">The <see cref="FileSystemEventArgs"/> instance containing the event data.</param>
        private void FileSystemWatcherOnChanged(Logger logger, FileSystemEventArgs e)
        {
            if (Watchers.TryGetValue(e.FullPath, out FileSystemWatcher fileSystemWatcher))
            {
                fileSystemWatcher.EnableRaisingEvents = false;
            }
            try
            {
                SettingsManager.Reload();
                logger.Log(ErrorLevel.DebugInfo, $"Settings reset because of file watcher event: {e.ChangeType}.");
            }
            finally
            {
                if (fileSystemWatcher != null)
                {
                    if (e.ChangeType != WatcherChangeTypes.Deleted)
                    {
                        fileSystemWatcher.EnableRaisingEvents = true;
                    }
                    else
                    {
                        fileSystemWatcher.Dispose();
                        Watchers.Remove(e.FullPath);
                    }
                }
            }
        }

        /// <summary>
        /// Handles exceptions raised by the file system watcher.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="errorEventArgs">The <see cref="ErrorEventArgs"/> instance containing the event data.</param>
        private void FileSystemWatcherOnError(Logger logger, ErrorEventArgs errorEventArgs)
        {
            string errorMessage = $"Error watching Live Integration settings XML file. Error: {errorEventArgs.GetException()}";
            logger.Log(ErrorLevel.Error, errorMessage);
        }

        /// <summary>
        /// Configures file watching.
        /// </summary>
        internal void SetUpWatching(Settings settings)
        {
            string file = GetSettingsFilePathPhysical(settings);
            if (!Watchers.ContainsKey(file))
            {
                var logger = GetLogger(settings);

                FileSystemWatcher fileSystemWatcher = new FileSystemWatcher
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                    Path = Path.GetDirectoryName(file),
                    Filter = Path.GetFileName(file)
                };

                // Add event handlers.
                fileSystemWatcher.Changed += (sender, e) => FileSystemWatcherOnChanged(logger, e);
                fileSystemWatcher.Deleted += (sender, e) => FileSystemWatcherOnChanged(logger, e);
                fileSystemWatcher.Renamed += (sender, e) => FileSystemWatcherOnChanged(logger, e);
                fileSystemWatcher.Error += (sender, e) => FileSystemWatcherOnError(logger, e);

                // Begin watching.
                fileSystemWatcher.EnableRaisingEvents = true;

                Watchers.Add(file, fileSystemWatcher);
            }
        }

        private Logger GetLogger(Settings settings)
        {
            return new Logger(settings);
        }
    }
}