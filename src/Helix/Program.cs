using System.ClientModel;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel;
using SpecForge.Agents;
using SpecForge.Filters;
using SpecForge.Shared;
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

kernel.FunctionInvocationFilters.Add(new FunctionCallReportingFilter());

var codingAgent = new CodingAgent(kernel);
var cancellationTokenSource = new CancellationTokenSource();

Console.CancelKeyPress += (sender, eventArgs) =>
{
    if (codingAgent.Running)
    {
        AnsiConsole.Write(new Markup("[aqua]Cancelling request...[/]"));
        cancellationTokenSource.Cancel();    
    }
    else
    {
        AnsiConsole.Write(new Markup("[aqua]Shutting down...[/]"));
        Environment.Exit(0);
    }
};

var logoContent = EmbeddedResource.ReadLines("Logo.txt");

foreach (var line in logoContent)
{
    AnsiConsole.WriteLine(line);
}

while (true)
{
    AnsiConsole.Write(new Rule().RuleStyle(Style.Parse("grey dim")));
    var userPrompt = AnsiConsole.Ask<String>("[green]Enter your prompt (or '/exit' to quit):[/] ");

    if (String.CompareOrdinal(userPrompt, "/exit") == 0)
    {
        AnsiConsole.Write(new Markup("[aqua]Shutting down...[/]"));
        break;
    }

    if (String.CompareOrdinal(userPrompt, "/clear") == 0)
    {
        AnsiConsole.Clear();
        continue;
    }

    await AnsiConsole.Status().StartAsync("Thinking...", async ctx =>
    {
        var callContext = new CodingAgentCallContext(ctx);
        var agentResponse = await codingAgent.InvokeAsync(userPrompt, callContext, cancellationTokenSource.Token);

        if (agentResponse != null)
        {
            AnsiConsole.Write(new Rule().RuleStyle(Style.Parse("grey dim")));
            AnsiConsole.WriteLine(agentResponse);
        }
    });
}