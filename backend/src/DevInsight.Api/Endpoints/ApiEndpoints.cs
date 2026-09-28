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
        MapAnalysisRuns(app.MapGroup("/api/analysis").WithTags("Analysis").RequireAuthorization());
        MapAnalysisResults(app.MapGroup("/api/analysis").WithTags("Analysis").RequireAuthorization());
        MapProjects(app.MapGroup("/api/projects").WithTags("Projects").RequireAuthorization());
        MapInsights(app);
    }

    private static void MapInsights(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/feedback/{analysisId:guid}", async (Guid analysisId, CurrentUser me, AnalysisQueries queries, CancellationToken ct) =>
                (await queries.GetAsync(me.Id, analysisId, ct)).Feedback.ToDtos())
            .WithTags("Feedback").RequireAuthorization().WithSummary("UC4: feedback items of an analysis.");

        app.MapGet("/api/dashboard", async (string? scope, CurrentUser me, GetDashboard dashboard, CancellationToken ct) =>
                (await dashboard.ExecuteAsync(me.Id, ScopeParser.Parse(scope), ct)).ToDto())
            .WithTags("Dashboard").RequireAuthorization().WithSummary("UC5: dashboard data for a scope.");

        app.MapGet("/api/portfolio/{handle}", async (string handle, HttpContext http, GetPortfolio portfolio, CancellationToken ct) =>
                (await portfolio.ExecuteAsync(handle, http.User.FindUserId(), ct)).ToDto())
            .WithTags("Portfolio").AllowAnonymous().WithSummary("UC6: public portfolio by GitHub login or user id.");
    }

    private static void MapProfile(RouteGroupBuilder me)
    {
        me.MapGet("/", async (CurrentUser user, ProfileUseCases profiles, CancellationToken ct) =>
            (await profiles.GetAsync(user.Id, ct)).ToDto());

        me.MapPut("/profile", async (UpdateProfileRequest body, CurrentUser user, ProfileUseCases profiles, CancellationToken ct) =>
            (await profiles.UpdateAsync(user.Id, new ProfileUpdate(body.Bio, body.LinkedInUrl, body.IsPortfolioPublic), ct)).ToDto());
    }

    private static void MapRepositories(RouteGroupBuilder repos)
    {
        repos.MapPost("/import", async (CurrentUser me, ImportRepositories import, CancellationToken ct) =>
                (await import.ExecuteAsync(me.Id, ct)).ToDto())
            .RequireRateLimiting(ImportRateLimit)
            .WithSummary("UC2: import repositories from GitHub (idempotent).");

        repos.MapGet("/", async (CurrentUser me, RepositoryQueries queries, CancellationToken ct) =>
            (await queries.ListAsync(me.Id, ct)).Select(r => r.ToDto()));

        repos.MapGet("/{id:guid}", async (Guid id, CurrentUser me, RepositoryQueries queries, CancellationToken ct) =>
            (await queries.GetAsync(me.Id, id, ct)).ToDto());

        repos.MapPatch("/{id:guid}/select", async (Guid id, SelectRepositoryRequest body, CurrentUser me, RepositoryQueries queries, CancellationToken ct) =>
                (await queries.SetSelectedAsync(me.Id, id, body.IsSelected, ct)).ToDto())
            .WithSummary("UC2.1: include or exclude a repository from the portfolio and aggregates.");
    }

    private static void MapAnalysisRuns(RouteGroupBuilder analysis)
    {
        analysis.MapPost("/run/{repoId:guid}", async (Guid repoId, string? scope, CurrentUser me, RequestAnalysis request, CancellationToken ct) =>
                Accepted(await request.ExecuteAsync(me.Id, repoId, ScopeParser.Parse(scope), ct)))
            .RequireRateLimiting(AnalysisRateLimit)
            .WithSummary("UC3: queue an analysis run (poll the returned run).");

        analysis.MapPost("/run-all", async (CurrentUser me, RequestAnalysis request, CancellationToken ct) =>
                TypedResults.Accepted("/api/analysis/runs", (await request.ExecuteForSelectedAsync(me.Id, ct)).Select(r => r.ToDto())))
            .RequireRateLimiting(AnalysisRateLimit)
            .WithSummary("Queue both scopes for every selected repository.");

        analysis.MapGet("/runs", async (CurrentUser me, AnalysisQueries queries, CancellationToken ct) =>
            (await queries.ListRecentRunsAsync(me.Id, ct)).Select(r => r.ToDto()));

        analysis.MapGet("/runs/{runId:guid}", async (Guid runId, CurrentUser me, AnalysisQueries queries, CancellationToken ct) =>
            (await queries.GetRunAsync(me.Id, runId, ct)).ToDto());
    }

    private static void MapAnalysisResults(RouteGroupBuilder analysis)
    {
        analysis.MapGet("/{repoId:guid}", async (Guid repoId, string? scope, CurrentUser me, AnalysisQueries queries, CancellationToken ct) =>
                (await queries.GetLatestAsync(me.Id, repoId, ScopeParser.Parse(scope), ct)).ToDto())
            .WithSummary("UC3: latest analysis of a repository in a scope.");

        analysis.MapGet("/{repoId:guid}/history", async (Guid repoId, string? scope, CurrentUser me, AnalysisQueries queries, CancellationToken ct) =>
                (await queries.GetHistoryAsync(me.Id, repoId, ScopeParser.Parse(scope), ct)).Select(s => s.ToDto()))
            .WithSummary("Score history (one point per analysis), oldest first.");
    }

    private static void MapProjects(RouteGroupBuilder projects)
    {
        projects.MapGet("/", async (CurrentUser me, ProjectUseCases useCases, CancellationToken ct) =>
            (await useCases.ListAsync(me.Id, ct)).Select(p => p.ToDto()));

        projects.MapPost("/", async Task<Created<ProjectDto>> (ProjectRequest body, CurrentUser me, ProjectUseCases useCases, CancellationToken ct) =>
        {
            var project = await useCases.CreateAsync(me.Id, body.ToDetails(), ct);
            return TypedResults.Created($"/api/projects/{project.Id}", project.ToDto());
        });

        projects.MapPut("/{id:guid}", async (Guid id, ProjectRequest body, CurrentUser me, ProjectUseCases useCases, CancellationToken ct) =>
            (await useCases.UpdateAsync(me.Id, id, body.ToDetails(), ct)).ToDto());

        projects.MapDelete("/{id:guid}", async (Guid id, CurrentUser me, ProjectUseCases useCases, CancellationToken ct) =>
        {
            await useCases.DeleteAsync(me.Id, id, ct);
            return Results.NoContent();
        });
    }

    private static Accepted<AnalysisRunDto> Accepted(AnalysisRun run) =>
        TypedResults.Accepted($"/api/analysis/runs/{run.Id}", run.ToDto());
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
