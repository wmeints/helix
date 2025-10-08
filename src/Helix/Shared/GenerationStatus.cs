namespace Helix.Shared;

public class GenerationStatus
{
    private static readonly string[] StatusMessages =
    [
        "Marinating...", "Thinking...", "Finagling...", "Brewing...", "Emerging...", "Baking..."
    ];
    
    public static string Random()
    {
        Random rand = new Random((int)DateTime.Now.Ticks);
        return StatusMessages[rand.Next(0, StatusMessages.Length)];
    }
}