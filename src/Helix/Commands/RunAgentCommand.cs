using System.ClientModel;
using Azure.AI.OpenAI;
using Helix.Agent;
using Helix.Endpoints;
using Microsoft.SemanticKernel;
using Spectre.Console.Cli;

namespace Helix.Commands;

public class RunAgentCommand : AsyncCommand<RunAgentCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RunAgentCommandSettings settings)
    {
        var helixDirectory = Path.Combine(settings.TargetDirectory ?? Directory.GetCurrentDirectory(), ".helix");

        if (!Directory.Exists(helixDirectory))
        {
            Directory.CreateDirectory(helixDirectory);
        }

        var connectionString = $"Data Source={Path.Combine(helixDirectory, "app.db")}";

        var builder = WebApplication.CreateBuilder(context.Arguments.ToArray());

        var languageModelEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ??
                                    throw new InvalidOperationException(
                                        "AZURE_OPENAI_ENDPOINT environment variable is not set.");

        var languageModelKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_KEY") ??
                               throw new InvalidOperationException("AZURE_OPENAI_KEY environment variable is not set.");

        var languageModelDeployment = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ??
                                      throw new InvalidOperationException(
                                          "AZURE_OPENAI_DEPLOYMENT environment variable is not set.");

        builder.Services.AddKernel()
            .AddAzureOpenAIChatCompletion(
                languageModelDeployment,
                new AzureOpenAIClient(new Uri(languageModelEndpoint),
                    new ApiKeyCredential(languageModelKey)
                )
            );

        // Include the command-line arguments in the application dependencies.
        // Other components can refer to these settings when needed.
        builder.Services.Configure<CodingAgentOptions>(options =>
        {
            options.TargetDirectory = settings.TargetDirectory ?? Directory.GetCurrentDirectory();
        });

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        builder.Services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        });

        builder.Services.AddCors(policy => policy.AddDefaultPolicy(policyBuilder =>
        {
            policyBuilder
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }));

        builder.Services.AddHostedService<OpenDefaultBrowser>();
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
        builder.Services.AddScoped<ICodingAgentFactory, CodingAgentFactory>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        app.UseCors();

        app.UseStaticFiles();

        app.MapHub<CodingAgentHub>("/hubs/coding");
        app.MapGetConversations();
        app.MapFallbackToFile("index.html");

        await app.RunAsync();

        return 0;
    }
}