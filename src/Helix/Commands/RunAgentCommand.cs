using System.ClientModel;
using Azure.AI.OpenAI;
using Helix.Agent;
using Helix.Endpoints;
using Microsoft.SemanticKernel;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Spectre.Console.Cli;

namespace Helix.Commands;

public class RunAgentCommand : AsyncCommand<RunAgentCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RunAgentCommandSettings settings)
    {
        var targetDirectory = GetTargetDirectory(settings.TargetDirectory);
        var builder = WebApplication.CreateBuilder(context.Arguments.ToArray());

        ConfigureLanguageModel(builder);

        // Include the command-line arguments in the application dependencies.
        // Other components can refer to these settings when needed.
        builder.Services.Configure<CodingAgentOptions>(options =>
        {
            options.TargetDirectory = targetDirectory;
        });

        ConfigureApplicationDatabase(builder, targetDirectory);

        builder.Services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
        });

        builder.Services.AddCors(policy => policy.AddDefaultPolicy(policyBuilder =>
        {
            policyBuilder
                .WithOrigins("http://localhost:5137")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }));

        builder.Services.AddHostedService<OpenDefaultBrowser>();
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
        builder.Services.AddScoped<ICodingAgentFactory, CodingAgentFactory>();
        builder.Services.AddSingleton<IAgentInstructions, AgentInstructions>();

        ConfigureOpenTelemetry(builder);

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

    private static void ConfigureApplicationDatabase(WebApplicationBuilder builder, string targetDirectory)
    {
        var helixDirectory = Path.Combine(targetDirectory, ".helix");

        if (!Directory.Exists(helixDirectory))
        {
            Directory.CreateDirectory(helixDirectory);
        }

        var connectionString = $"Data Source={Path.Combine(helixDirectory, "app.db")}";

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));
    }

    private string GetTargetDirectory(string? targetDirectory)
    {
        if (string.IsNullOrEmpty(targetDirectory))
        {
            targetDirectory = Directory.GetCurrentDirectory();
        }

        if (targetDirectory != null && !Directory.Exists(targetDirectory))
        {
            throw new DirectoryNotFoundException($"The specified target directory '{targetDirectory}' does not exist.");
        }

        return targetDirectory!;
    }

    private void ConfigureLanguageModel(WebApplicationBuilder builder)
    {
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
    }

    private void ConfigureOpenTelemetry(WebApplicationBuilder builder)
    {
        var serviceName = builder.Configuration.GetValue<string>("OpenTelemetry:ServiceName") ?? "Helix";
        var serviceVersion = builder.Configuration.GetValue<string>("OpenTelemetry:ServiceVersion") ?? "1.0.0";
        var otlpEndpoint = builder.Configuration.GetValue<string>("OpenTelemetry:OtlpEndpoint") ?? "http://localhost:4317";

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("Microsoft.SemanticKernel*")
                .AddEntityFrameworkCoreInstrumentation(options =>
                {
                    options.SetDbStatementForText = true;
                    options.SetDbStatementForStoredProcedure = true;
                })
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                }))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddMeter("Microsoft.SemanticKernel*")
                .AddOtlpExporter(options =>
                {
                    options.Endpoint = new Uri(otlpEndpoint);
                }));

        AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);
    }
}