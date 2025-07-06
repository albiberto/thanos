using System.Text.Json;
using Thanos.Model;

namespace Thanos;

public static class Api
{
    public static void AddEndpoints(this WebApplication app)
    {
        app.MapGet("/", () => new
        {
            apiversion = "1",
            author = "YourName",
            color = "#FF0000",
            head = "default",
            tail = "default"
        });

        // Move endpoint - IL CORE DELL'APPLICAZIONE
        app.MapPost("/move", (MoveRequest request) =>
        {
            Console.WriteLine(JsonSerializer.Serialize(request));
            // === QUI VA LA TUA LOGICA DI MOVIMENTO ===
        });
    }
}