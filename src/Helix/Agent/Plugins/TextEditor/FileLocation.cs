using System.Runtime.InteropServices;

namespace Helix.Agent.Plugins.TextEditor;

public static class FileLocation
{
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