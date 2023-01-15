using Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Logging;
using System;
using System.Globalization;

namespace Dynamicweb.Ecommerce.DynamicwebLiveIntegration.Licensing
{
    internal class VersionInfo
    {
        internal Version AppVersion { get; private set; }
        internal int CustomVersion { get; private set; }
        internal string ServerVersion { get; private set; }

        internal VersionInfo()
        {
            CustomVersion = -1;
        }

        internal static bool TryParse(string version, Logger logger, out VersionInfo result)
        {
            result = new VersionInfo();
            if (version == null)
            {
                logger.Log(ErrorLevel.DebugInfo, "Version Error: String to be parsed is null.");
                return false;
            }

            string v = version.ToLower();
            if (v.IndexOf("_nav") > 0)
            {
                string[] parts = v.Split(new string[] { "_nav" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    result.ServerVersion = parts[1];
                }
                v = parts[0];
            }

            string[] parsedComponents = v.Split(new char[] { '.' });
            int parsedComponentsLength = parsedComponents.Length;
            if ((parsedComponentsLength < 2) || (parsedComponentsLength > 5))
            {
                logger.Log(ErrorLevel.DebugInfo, $"Version Error: Bad number of components in '{version}'.");
                return false;
            }

            if (!TryParseComponent(parsedComponents[0], "major version", logger, out int major))
            {
                return false;
            }

            if (!TryParseComponent(parsedComponents[1], "minor version", logger, out int minor))
            {
                return false;
            }

            parsedComponentsLength -= 2;

            if (parsedComponentsLength > 0)
            {
                if (!TryParseComponent(parsedComponents[2], "build", logger, out int build))
                {
                    return false;
                }

                parsedComponentsLength--;

                if (parsedComponentsLength > 0)
                {
                    if (!TryParseComponent(parsedComponents[3], "revision", logger, out int revision))
                    {
                        return false;
                    }
                    else
                    {
                        result.AppVersion = new Version(major, minor, build, revision);
                    }
                }
                else
                {
                    result.AppVersion = new Version(major, minor, build);
                }

                parsedComponentsLength--;

                if (parsedComponentsLength > 0)
                {
                    if (!TryParseComponent(parsedComponents[4], "custom version", logger, out int customVersion))
                    {
                        return false;
                    }
                    else
                    {
                        result.CustomVersion = customVersion;
                    }
                }
            }
            else
            {
                result.AppVersion = new Version(major, minor);
            }

            return true;
        }

        internal static bool TryParseServerVersion(string serverVersion, Logger logger, out Version result)
        {
            result = new Version();
            if (serverVersion == null)
            {
                logger.Log(ErrorLevel.DebugInfo, "Version Error: String to be parsed is null.");
                return false;
            }
            string[] parsedComponents = serverVersion.Split(new char[] { '.' });
            int parsedComponentsLength = parsedComponents.Length;

            if (parsedComponentsLength == 0)
            {
                logger.Log(ErrorLevel.DebugInfo, $"Version Error: Bad number of components in '{serverVersion}'.");
                return false;
            }
            if (!TryParseComponent(parsedComponents[0], "major version", logger, out int major))
            {
                return false;
            }
            int minor = 0;
            if (parsedComponentsLength > 1)
            {
                TryParseComponent(parsedComponents[1], "minor version", logger, out minor);
            }
            result = new Version(major, minor, 0, 0);
            return true;
        }

        private static bool TryParseComponent(string component, string componentName, Logger logger, out int parsedComponent)
        {
            if (!int.TryParse(component, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedComponent))
            {
                logger.Log(ErrorLevel.DebugInfo, $"Version Error: Non-integer value '{component}' in '{componentName}'.");
                return false;
            }

            if (parsedComponent < 0)
            {
                logger.Log(ErrorLevel.DebugInfo, $"Version Error: Number '{component}' is out of range in '{componentName}'.");
                return false;
            }

            return true;
        }
    }
}
