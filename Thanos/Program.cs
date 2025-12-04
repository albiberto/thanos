using Thanos;

var builder = WebApplication.CreateBuilder(args);

Bootstrapper.OverrideConsoleStandardOutput();

var app = builder.Build();

var agent = Bootstrapper.BuildColdPath(Constants.Cores, Constants.Nodes);
app.Lifetime.ApplicationStopping.Register(() => agent.Dispose());
app.MapGetInfo().MapStart(agent).MapMove(agent).MapEnd(agent);

await app.RunAsync();
