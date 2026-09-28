using DevInsight.Api.Auth;
using DevInsight.Api.Endpoints;
using DevInsight.Api.Infrastructure;
using DevInsight.Application;
using DevInsight.Infrastructure;
using DevInsight.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiServices(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSecurityHeaders();
app.UseDefaultFiles();
app.UseStaticFiles();
// Cross-origin SPA (GitHub Pages) only; same-origin hosting needs no CORS.
if (app.Services.GetRequiredService<IOptions<FrontendOptions>>().Value.Origin is not null)
{
    app.UseCors();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapOpenApi();
app.MapScalarApiReference(options => options.WithTitle("DevInsight API"));
app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapApiEndpoints();

// Unknown API routes are 404s; every other unknown path is a client-side route of the Angular SPA.
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

if (app.Configuration.GetValue("Database:MigrateOnStartup", true))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<DevInsightDbContext>().Database.MigrateAsync();
}

await app.RunAsync();

/// <summary>Entry point, exposed for WebApplicationFactory in the integration tests.</summary>
public partial class Program;
