using DevInsight.Application.Abstractions;
using DevInsight.Domain.Analyses;
using DevInsight.Domain.Repositories;

namespace DevInsight.Application.Dashboard;

public sealed record RepositorySummary(Repository Repository, Analysis? LatestAnalysis);

public sealed record FeedbackHighlight(Guid RepositoryId, string RepositoryName, Feedback Feedback);

public sealed record DashboardView(
    AnalysisScope Scope,
    int RepositoryCount,
    int SelectedCount,
    int AnalyzedCount,
    Scores? AverageScores,
    IReadOnlyList<RepositorySummary> Repositories,
    IReadOnlyList<LanguageShare> Languages,
    IReadOnlyList<ActivityWeek> Activity,
    IReadOnlyList<ScorePoint> ScoreEvolution,
    CommitQualitySummary CommitQuality,
    IReadOnlyList<FeedbackHighlight> TopFeedback);

/// <summary>UC5: everything the dashboard renders, for one scope. Aggregates cover selected repositories only.</summary>
public sealed class GetDashboard(IRepositoryStore repositories, IAnalysisStore analyses)
{
    private const int TopFeedbackCount = 8;

    public async Task<DashboardView> ExecuteAsync(Guid userId, AnalysisScope scope, CancellationToken cancellationToken)
    {
        var all = await RepositoryInsights.LoadAsync(
            analyses, await repositories.ListForUserAsync(userId, cancellationToken), scope, cancellationToken);
        var selected = all.Where(r => r.IsSelected);
        return new DashboardView(
            scope,
            all.Repositories.Count,
            selected.Repositories.Count,
            selected.Latest.Count,
            Insights.AverageScores(selected.Latest),
            [.. all.Repositories.OrderByDescending(r => r.IsSelected).ThenByDescending(r => r.LastActivity).Select(all.SummaryOf)],
            Insights.Languages(selected.Repositories),
            Insights.CombinedTimeline(selected.Latest),
            Insights.ScoreEvolution(selected.History),
            Insights.CommitQuality(selected.Latest),
            TopFeedback(selected));
    }

    private static List<FeedbackHighlight> TopFeedback(RepositoryInsights selected) =>
    [
        .. selected.Highlights(f => !f.IsStrength)
            .OrderByDescending(h => h.Feedback.Severity)
            .ThenBy(h => h.RepositoryName, StringComparer.OrdinalIgnoreCase)
            .Take(TopFeedbackCount),
    ];
}
