namespace Helix.Shared;

public class EmbeddedResource
{
    public static string Read(string resourceName)
    {
        var fullResourceName = $"Helix.{resourceName}";
        var resourceStream = typeof(EmbeddedResource).Assembly.GetManifestResourceStream(fullResourceName);
        using var reader = new StreamReader(resourceStream!);
        
        return reader.ReadToEnd();
    }

    public static List<String> ReadLines(string resourceName)
    {
        var resourceContent = Read(resourceName);
        var lines = resourceContent.Split('\n').Select(line => line.TrimEnd()).ToList();

        return lines;
    }
}
