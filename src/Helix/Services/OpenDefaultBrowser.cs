using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Helix.Services;

public class OpenDefaultBrowser: IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var url = "http://localhost:5000";
        
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
        catch
        {
            // Silently fail if browser cannot be opened
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Do nothing.
        return Task.CompletedTask;
    }
}