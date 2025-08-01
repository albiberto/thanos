using System.Runtime.CompilerServices;
using Thanos;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

// Initialize GameManager during application bootstrapping
RuntimeHelpers.RunClassConstructor(typeof(GameEngine).TypeHandle);

app.MapGet("/", () => Results.Ok);

app.MapPost("/start", () => Results.Ok());

app.MapPost("/end", () => Results.Ok());

app.MapPost("/move", async context =>
{
    await Task.Delay(100); // Simulate processing delay
});

app.Run();
