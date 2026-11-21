using System.Text.Json.Serialization;
using Bookly.Api.Books;
using Bookly.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BooklyDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Bookly")));
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BooklyDbContext>();
    db.Database.Migrate();
}
app.UseExceptionHandler();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapBookEndpoints();
// Nieznane /api/* → 404, a nie index.html
app.MapFallback("/api/{**rest}", () => Results.NotFound());
// Wszystko inne → aplikacja React (routing po stronie przeglądarki)
app.MapFallbackToFile("index.html");

app.Run();