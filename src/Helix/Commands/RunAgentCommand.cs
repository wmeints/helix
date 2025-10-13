using Helix.Agent;
using Spectre.Console.Cli;

namespace Helix.Commands;

public class RunAgentCommand: AsyncCommand<RunAgentCommandSettings>
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

        // Include the command-line arguments in the application dependencies.
        // Other components can refer to these settings when needed.
        builder.Services.Configure<CodingAgentOptions>(options =>
        {
            options.TargetDirectory = settings.TargetDirectory ?? Directory.GetCurrentDirectory();
        });
        
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(connectionString));

        builder.Services.AddSignalR();
        builder.Services.AddCors();

        builder.Services.AddHostedService<OpenDefaultBrowser>();
        builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
        builder.Services.AddScoped<IConversationRepository, ConversationRepository>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();
        }

        app.UseStaticFiles();
        app.UseCors();

        app.MapHub<CodingAgentHub>("/hubs/coding");
        app.MapFallbackToFile("index.html");

        await app.RunAsync();

        return 0;
    }
}