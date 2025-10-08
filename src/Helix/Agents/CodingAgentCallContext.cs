using Spectre.Console;

namespace Helix.Agents;

public class CodingAgentCallContext
{
    private StatusContext _statusContext;

    public CodingAgentCallContext(StatusContext statusContext)
    {
        _statusContext = statusContext;
    }

    public void UpdateStatus(string statusMessage)
    {
        _statusContext.Status(statusMessage);
    }
}
