using Bookly.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BooklyDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Bookly")));
    
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();
    db.Database.Migrate();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

// Nieznane /api/* → 404, a nie index.html
app.MapFallback("/api/{**rest}", () => Results.NotFound());
// Wszystko inne → aplikacja React (routing po stronie przeglądarki)
app.MapFallbackToFile("index.html");

app.Run();