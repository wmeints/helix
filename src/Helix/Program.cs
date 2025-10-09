using System.ClientModel;
using Azure.AI.OpenAI;
using Helix.Agent;
using Helix.Terminal;
using Microsoft.SemanticKernel;
using Spectre.Console;

var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT");
var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

var kernel = Kernel.CreateBuilder()
    .AddAzureOpenAIChatClient(deploymentName!,
        new AzureOpenAIClient(new Uri(endpoint!),
            new ApiKeyCredential(apiKey!))
    )
    .Build();

var codingAgent = new CodingAgent(kernel);
var codingAgentInterface = new AgentInterface(AnsiConsole.Console, codingAgent);

Console.CancelKeyPress += (sender, eventArgs) =>
{
    if (codingAgent.Running)
    {
        codingAgent.CancelRequest();
    }
    else
    {
        AnsiConsole.Write(new Markup("[aqua]Shutting down...[/]"));
        Environment.Exit(0);
    }
};

ApplicationLogo.Render();
await codingAgentInterface.RunAsync();