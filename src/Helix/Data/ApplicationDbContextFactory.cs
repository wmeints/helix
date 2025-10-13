using Microsoft.EntityFrameworkCore.Design;

namespace Helix.Data;

/// <summary>
/// Design time data context factory
/// </summary>
/// <remarks>
/// We need to use this factory to create the database during design time (e.g. for migrations)
/// because we've changed how the application runs. The .NET EF Core tools can't handle the fact that we're using
/// a custom startup process with Spectre.Console.CLI
/// </remarks>
public class ApplicationDbContextFactory: IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var helixDirectory = Path.Combine(Directory.GetCurrentDirectory(), ".helix");

        if (!Directory.Exists(helixDirectory))
        {
            Directory.CreateDirectory(helixDirectory);
        }
        
        var connectionString = $"Data Source={Path.Combine(helixDirectory, "app.db")}";
        
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connectionString).Options;
        var dbContext = new ApplicationDbContext(options);

        return dbContext;
    }
}