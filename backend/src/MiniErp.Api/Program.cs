using Microsoft.EntityFrameworkCore;
using MiniErp.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt => opt
    .UseNpgsql(builder.Configuration.GetConnectionString("Default"))
    .UseSnakeCaseNamingConvention());

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" })).ExcludeFromDescription();

app.Run();

public partial class Program;
