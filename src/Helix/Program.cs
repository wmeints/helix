using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using Helix.Data;
using Helix.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure database context
var helixDirectory = Path.Combine(Directory.GetCurrentDirectory(), ".helix");
Directory.CreateDirectory(helixDirectory);

var connectionString = $"Data Source={Path.Combine(helixDirectory, "app.db")}";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddSignalR();

// Open the default browser on application startup.
builder.Services.AddHostedService<OpenDefaultBrowser>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
