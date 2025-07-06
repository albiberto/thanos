using Thanos;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureKestrel();
builder.ConfigureHttpJsonSerializer();
builder.ConfigureGlobJsonSerializer();

builder.AddRouting();
builder.AddLogging();

builder.AddMonteCarlo();

var app = builder.Build();

// ===== MIDDLEWARE OTTIMIZZATO =====
if (app.Environment.IsProduction())
{
    app.UseRouting();
}
else
{
    app.UseDeveloperExceptionPage();
    app.UseRouting();
}

// ===== ADD ENDPOINTS =====
app.AddEndpoints();

// ===== PRE-WARM =====
Console.WriteLine("🔥 Pre-warming all systems for MAXIMUM VELOCITY...");
_ = app.Services.GetRequiredService<MonteCarlo>(); // Force JIT compilation di tutti i metodi critici

// ===== AVVIO DELL'APPLICAZIONE =====
app.Run();