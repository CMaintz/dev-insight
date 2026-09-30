using DevInsight.Application.Abstractions;
using DevInsight.Application.Common;
using DevInsight.Application.Dashboard;
using DevInsight.Domain.Analyses;
using DevInsight.Domain.Projects;
using DevInsight.Domain.Users;

namespace DevInsight.Application.Portfolio;

public sealed record PortfolioView(
    User Owner,
    Scores? Scores,
    IReadOnlyList<RepositorySummary> Repositories,
    IReadOnlyList<PortfolioProject> Projects,
    IReadOnlyList<LanguageShare> Languages,
    IReadOnlyList<ActivityWeek> Activity,
    IReadOnlyList<ScorePoint> ScoreEvolution,
    IReadOnlyList<FeedbackHighlight> Strengths);

public sealed record PortfolioProject(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<string> ImageUrls,
    IReadOnlyList<Guid> LinkedRepositoryIds);

/// <summary>
/// UC6: the public portfolio. Shows selected repositories only, scored in USER_CONTRIBUTION scope
/// (the user's own work). Private repositories are never shown, even when selected. Only strengths
/// are shown publicly; improvement feedback stays private.
/// </summary>
public sealed class GetPortfolio(
    IUserStore users,
    IRepositoryStore repositories,
    IAnalysisStore analyses,
    IProjectStore projects)
{
    public const AnalysisScope PortfolioScope = AnalysisScope.UserContribution;

    /// <param name="handle">GitHub login or user id.</param>
    /// <param name="viewerId">The signed-in viewer, if any — owners can preview an unpublished portfolio.</param>
    public async Task<PortfolioView> ExecuteAsync(string handle, Guid? viewerId, CancellationToken cancellationToken)
    {
        var owner = await FindVisibleOwnerAsync(handle, viewerId, cancellationToken);
        var shown = (await repositories.ListForUserAsync(owner.Id, cancellationToken)).Where(r => r.IsSelected && !r.IsPrivate).ToList();
        var insights = await RepositoryInsights.LoadAsync(analyses, shown, PortfolioScope, cancellationToken);
        var shownIds = shown.Select(r => r.Id).ToHashSet();
        return new PortfolioView(
            owner,
            Insights.AverageScores(insights.Latest),
            [.. shown.Select(insights.SummaryOf).OrderByDescending(s => s.LatestAnalysis?.OverallScore ?? -1).ThenByDescending(s => s.Repository.Stars)],
            [.. (await projects.ListForUserAsync(owner.Id, cancellationToken)).OrderBy(p => p.SortOrder).Select(p => ToPublic(p, shownIds))],
            Insights.Languages(shown),
            Insights.CombinedTimeline(insights.Latest),
            Insights.ScoreEvolution(insights.History),
            [.. insights.Highlights(f => f.IsStrength)]);
    }

    /// <summary>An unpublished portfolio is indistinguishable from a missing one, except to its owner.</summary>
    private async Task<User> FindVisibleOwnerAsync(string handle, Guid? viewerId, CancellationToken cancellationToken)
    {
        var owner = Guid.TryParse(handle, out var id)
            ? await users.GetAsync(id, cancellationToken)
            : await users.FindByLoginAsync(handle, cancellationToken);
        return owner is not null && (owner.IsPortfolioPublic || owner.Id == viewerId)
            ? owner
            : throw new NotFoundException("Portfolio", handle);
    }

    /// <summary>A project may link a repository that was later deselected; the public view must not reveal it.</summary>
    private static PortfolioProject ToPublic(Project project, HashSet<Guid> visibleRepoIds) => new(
        project.Id,
        project.Name,
        project.Description,
        project.ImageUrls,
        [.. project.LinkedRepositoryIds.Where(visibleRepoIds.Contains)]);
}
