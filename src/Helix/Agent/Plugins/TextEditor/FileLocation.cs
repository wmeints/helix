using System.Runtime.InteropServices;

namespace Helix.Agent.Plugins.TextEditor;

/// <summary>
/// Resolves and validates file locations for the agent.
/// </summary>
public static class FileLocation
{
    /// <summary>
    /// Resolve the file location to an absolute path.
    /// </summary>
    /// <param name="relativePath"></param>
    /// <returns>Returns the absolute file location for the file.</returns>
    /// <remarks>
    /// On Windows, this will expand %USERPROFILE% and %APP_DATA% environment variables.
    /// On Linux and MacOS, this will expand the ~ to the user's home directory.
    /// </remarks>
    public static string Resolve(string relativePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var appDataPath = Environment.GetEnvironmentVariable("APP_DATA");
            var userProfilePath = Environment.GetEnvironmentVariable("USERPROFILE");

            relativePath = relativePath.Replace("%USERPROFILE%", userProfilePath ?? "");
            relativePath = relativePath.Replace("%APP_DATA%", appDataPath ?? "");
        }
        else
        {
            var homeFolder = Environment.GetEnvironmentVariable("HOME");
            relativePath = relativePath.Replace("~", homeFolder);
        }

        return Path.GetFullPath(relativePath);
    }
}