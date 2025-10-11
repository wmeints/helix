using Microsoft.EntityFrameworkCore;
using Helix.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure database context
var helixDirectory = Path.Combine(Directory.GetCurrentDirectory(), ".helix");
Directory.CreateDirectory(helixDirectory);

var connectionString = $"Data Source={Path.Combine(helixDirectory, "app.db")}";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddSignalR();

var app = builder.Build();

// Ensure database is created and up to date
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
