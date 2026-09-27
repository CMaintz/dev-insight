using DevInsight.Api.Auth;
using DevInsight.Api.Contracts;
using DevInsight.Application.Analyses;
using DevInsight.Application.Dashboard;
using DevInsight.Application.Portfolio;
using DevInsight.Application.Profile;
using DevInsight.Application.Projects;
using DevInsight.Application.Repositories;
using DevInsight.Domain.Analyses;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DevInsight.Api.Endpoints;

internal static class ApiEndpoints
{
    public const string AnalysisRateLimit = "analysis";
    public const string ImportRateLimit = "import";

    public static void MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        MapProfile(app.MapGroup("/api/me").WithTags("Profile").RequireAuthorization());
        MapRepositories(app.MapGroup("/api/repos").WithTags("Repositories").RequireAuthorization());
        MapAnalysis(app.MapGroup("/api/analysis").WithTags("Analysis").RequireAuthorization());
        MapProjects(app.MapGroup("/api/projects").WithTags("Projects").RequireAuthorization());

        app.MapGet("/api/feedback/{analysisId:guid}", async (Guid analysisId, HttpContext http, AnalysisQueries queries, CancellationToken ct) =>
                (await queries.GetAsync(http.User.GetUserId(), analysisId, ct)).Feedback.ToDtos())
            .WithTags("Feedback").RequireAuthorization().WithSummary("UC4: feedback items of an analysis.");

        app.MapGet("/api/dashboard", async (string? scope, HttpContext http, GetDashboard dashboard, CancellationToken ct) =>
                (await dashboard.ExecuteAsync(http.User.GetUserId(), ScopeParser.Parse(scope), ct)).ToDto())
            .WithTags("Dashboard").RequireAuthorization().WithSummary("UC5: dashboard data for a scope.");

        app.MapGet("/api/portfolio/{handle}", async (string handle, HttpContext http, GetPortfolio portfolio, CancellationToken ct) =>
                (await portfolio.ExecuteAsync(handle, http.User.FindUserId(), ct)).ToDto())
            .WithTags("Portfolio").AllowAnonymous().WithSummary("UC6: public portfolio by GitHub login or user id.");
    }

    private static void MapProfile(RouteGroupBuilder me)
    {
        me.MapGet("/", async (HttpContext http, ProfileUseCases profiles, CancellationToken ct) =>
            (await profiles.GetAsync(http.User.GetUserId(), ct)).ToDto());

        me.MapPut("/profile", async (UpdateProfileRequest body, HttpContext http, ProfileUseCases profiles, CancellationToken ct) =>
            (await profiles.UpdateAsync(http.User.GetUserId(), new ProfileUpdate(body.Bio, body.LinkedInUrl, body.IsPortfolioPublic), ct)).ToDto());
    }

    private static void MapRepositories(RouteGroupBuilder repos)
    {
        repos.MapPost("/import", async (HttpContext http, ImportRepositories import, CancellationToken ct) =>
            {
                var summary = await import.ExecuteAsync(http.User.GetUserId(), ct);
                return new ImportSummaryDto(summary.Imported, summary.Updated, summary.Total);
            })
            .RequireRateLimiting(ImportRateLimit)
            .WithSummary("UC2: import repositories from GitHub (idempotent).");

        repos.MapGet("/", async (HttpContext http, RepositoryQueries queries, CancellationToken ct) =>
            (await queries.ListAsync(http.User.GetUserId(), ct)).Select(r => r.ToDto()));

        repos.MapGet("/{id:guid}", async (Guid id, HttpContext http, RepositoryQueries queries, CancellationToken ct) =>
            (await queries.GetAsync(http.User.GetUserId(), id, ct)).ToDto());

        repos.MapPatch("/{id:guid}/select", async (Guid id, SelectRepositoryRequest body, HttpContext http, RepositoryQueries queries, CancellationToken ct) =>
                (await queries.SetSelectedAsync(http.User.GetUserId(), id, body.IsSelected, ct)).ToDto())
            .WithSummary("UC2.1: include or exclude a repository from the portfolio and aggregates.");
    }

    private static void MapAnalysis(RouteGroupBuilder analysis)
    {
        analysis.MapPost("/run/{repoId:guid}", async (Guid repoId, string? scope, HttpContext http, RequestAnalysis request, CancellationToken ct) =>
            {
                var run = await request.ExecuteAsync(http.User.GetUserId(), repoId, ScopeParser.Parse(scope), ct);
                return TypedResults.Accepted($"/api/analysis/runs/{run.Id}", run.ToDto());
            })
            .RequireRateLimiting(AnalysisRateLimit)
            .WithSummary("UC3: queue an analysis run (poll the returned run).");

        analysis.MapPost("/run-all", async (HttpContext http, RequestAnalysis request, CancellationToken ct) =>
                TypedResults.Accepted("/api/analysis/runs", (await request.ExecuteForSelectedAsync(http.User.GetUserId(), ct)).Select(r => r.ToDto())))
            .RequireRateLimiting(AnalysisRateLimit)
            .WithSummary("Queue both scopes for every selected repository.");

        analysis.MapGet("/runs", async (HttpContext http, AnalysisQueries queries, CancellationToken ct) =>
            (await queries.ListRecentRunsAsync(http.User.GetUserId(), ct)).Select(r => r.ToDto()));

        analysis.MapGet("/runs/{runId:guid}", async (Guid runId, HttpContext http, AnalysisQueries queries, CancellationToken ct) =>
            (await queries.GetRunAsync(http.User.GetUserId(), runId, ct)).ToDto());

        analysis.MapGet("/{repoId:guid}", async (Guid repoId, string? scope, HttpContext http, AnalysisQueries queries, CancellationToken ct) =>
                (await queries.GetLatestAsync(http.User.GetUserId(), repoId, ScopeParser.Parse(scope), ct)).ToDto())
            .WithSummary("UC3: latest analysis of a repository in a scope.");

        analysis.MapGet("/{repoId:guid}/history", async (Guid repoId, string? scope, HttpContext http, AnalysisQueries queries, CancellationToken ct) =>
                (await queries.GetHistoryAsync(http.User.GetUserId(), repoId, ScopeParser.Parse(scope), ct)).Select(s => s.ToDto()))
            .WithSummary("Score history (one point per analysis), oldest first.");
    }

    private static void MapProjects(RouteGroupBuilder projects)
    {
        projects.MapGet("/", async (HttpContext http, ProjectUseCases useCases, CancellationToken ct) =>
            (await useCases.ListAsync(http.User.GetUserId(), ct)).Select(p => p.ToDto()));

        projects.MapPost("/", async Task<Created<ProjectDto>> (ProjectRequest body, HttpContext http, ProjectUseCases useCases, CancellationToken ct) =>
        {
            var project = await useCases.CreateAsync(http.User.GetUserId(), body.ToDetails(), ct);
            return TypedResults.Created($"/api/projects/{project.Id}", project.ToDto());
        });

        projects.MapPut("/{id:guid}", async (Guid id, ProjectRequest body, HttpContext http, ProjectUseCases useCases, CancellationToken ct) =>
            (await useCases.UpdateAsync(http.User.GetUserId(), id, body.ToDetails(), ct)).ToDto());

        projects.MapDelete("/{id:guid}", async (Guid id, HttpContext http, ProjectUseCases useCases, CancellationToken ct) =>
        {
            await useCases.DeleteAsync(http.User.GetUserId(), id, ct);
            return Results.NoContent();
        });
    }
}

internal static class ScopeParser
{
    /// <summary>Accepts the spec's <c>repo|user</c> as well as the enum names; defaults to the whole repository.</summary>
    public static AnalysisScope Parse(string? scope) => scope?.Trim().ToLowerInvariant() switch
    {
        null or "" or "repo" => AnalysisScope.Repo,
        "user" or "usercontribution" or "user_contribution" => AnalysisScope.UserContribution,
        _ => throw new BadHttpRequestException($"Unknown scope '{scope}'. Use 'repo' or 'user'."),
    };
}
