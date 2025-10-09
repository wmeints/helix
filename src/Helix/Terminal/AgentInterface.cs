using Helix.Agent;
using Microsoft.SemanticKernel;
using Spectre.Console;

namespace Helix.Terminal;

public class AgentInterface(IAnsiConsole console, CodingAgent agent)
{
    public async Task RunAsync()
    {
        while (true)
        {
            console.Write(new Rule().RuleStyle("grey dim"));
            var userPrompt = console.Ask<string>("[green]> [/]");

            if (string.CompareOrdinal("/exit", userPrompt) == 0)
            {
                break;
            }

            if (string.CompareOrdinal("/help", userPrompt) == 0)
            {
                DisplayHelpMessage();
                continue;
            }

            await agent.InvokeAsync(userPrompt, this);
        }
    }

    public void AppendRule()
    {
        console.Write(new Rule().RuleStyle("grey dim"));
    }

    public void AppendMessage(string content)
    {
        console.Write(new Rule().RuleStyle("grey dim"));
        console.WriteLine(content);
    }

    public void AppendMessageFragment(string content)
    {
        console.Write(content);
    }

    public void CompleteMessage()
    {
        console.WriteLine();
    }

    public void AppendToolCallInfo(string toolName, KernelArguments toolArgs)
    {
        console.Write(new Rule($"Tool: {toolName}").RuleStyle("grey dim"));
        console.Write(new Rule().RuleStyle("grey dim"));

        foreach (var toolArg in toolArgs)
        {
            console.Write($"[grey]{toolArg.Key}: {toolArg.Value}[/]");
            console.WriteLine();
        }
    }

    private void DisplayHelpMessage()
    {
        console.Write(new Rule().RuleStyle("grey dim"));
        console.Write("[grey]Available commands:[/]");
        console.WriteLine();
        console.Write("[grey]/help - Show this help message[/]");
        console.WriteLine();
        console.Write("[grey]/exit - Exit the application[/]");
        console.WriteLine();
        console.Write("[grey]Press Ctrl+C to cancel the current operation.[/]");
        console.WriteLine();
        console.WriteLine();
    }
}