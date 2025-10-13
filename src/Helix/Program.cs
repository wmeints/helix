var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<RunAgentCommand>("run");
});

app.SetDefaultCommand<RunAgentCommand>();

return await app.RunAsync(args);