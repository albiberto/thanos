using Thanos;

var builder = WebApplication.CreateBuilder(args);

var logFileStream = new FileStream("app_log.txt", FileMode.Create, FileAccess.ReadWrite);
var logStreamWriter = new StreamWriter(logFileStream);

logStreamWriter.AutoFlush = true;

Console.SetOut(logStreamWriter);
Console.SetError(logStreamWriter);

var app = builder.Build();

var agent = new BattleSnakeAgent();
app.Lifetime.ApplicationStopping.Register(() => agent.Dispose());

app
    .MapGetInfo()
    .MapStart(agent)
    .MapMove(agent)
    .MapEnd(agent);

await app.RunAsync();