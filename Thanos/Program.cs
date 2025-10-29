using Thanos;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var agent = new BattleSnakeAgent();
app.Lifetime.ApplicationStopping.Register(() => agent.Dispose());

app
    .MapGetInfo()
    .MapStart(agent)
    .MapMove(agent)
    .MapEnd(agent);

await app.RunAsync();