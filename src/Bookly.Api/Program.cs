var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

// Nieznane /api/* → 404, a nie index.html
app.MapFallback("/api/{**rest}", () => Results.NotFound());
// Wszystko inne → aplikacja React (routing po stronie przeglądarki)
app.MapFallbackToFile("index.html");

app.Run();