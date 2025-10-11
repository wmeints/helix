using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Helix.Services;

/// <summary>
/// Opens the default browser when the application starts.
/// </summary>
public class OpenDefaultBrowser(ILogger<OpenDefaultBrowser> logger): IHostedService
{
    /// <summary>
    /// Start the hosted service
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
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
            logger.LogWarning("Failed to open URL in the default browser.");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stop the hosted service
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        // Do nothing.
        return Task.CompletedTask;
    }
}