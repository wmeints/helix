using Helix.Shared;

namespace Helix.Terminal;

public class ApplicationLogo
{
    public static void Render()
    {
        var logoContent = EmbeddedResource.ReadLines("Terminal.Logo.txt");

        foreach (var line in logoContent)
        {
            Console.WriteLine(line);
        }
    }
}