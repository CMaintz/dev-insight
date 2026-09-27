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
        var all = await repositories.ListForUserAsync(userId, cancellationToken);
        var selected = all.Where(r => r.IsSelected).ToList();
        var latest = await analyses.GetLatestForRepositoriesAsync([.. all.Select(r => r.Id)], scope, cancellationToken);
        var latestByRepo = latest.ToDictionary(a => a.RepositoryId);
        var selectedIds = selected.Select(r => r.Id).ToHashSet();
        var selectedLatest = latest.Where(a => selectedIds.Contains(a.RepositoryId)).ToList();
        var history = await analyses.GetScoreHistoryAsync(selectedIds, scope, cancellationToken);

        return new DashboardView(
            scope,
            all.Count,
            selected.Count,
            selectedLatest.Count,
            Insights.AverageScores(selectedLatest),
            [.. all
                .OrderByDescending(r => r.IsSelected)
                .ThenByDescending(r => r.LastActivity)
                .Select(r => new RepositorySummary(r, latestByRepo.GetValueOrDefault(r.Id)))],
            Insights.Languages(selected),
            Insights.CombinedTimeline(selectedLatest),
            Insights.ScoreEvolution(history),
            Insights.CommitQuality(selectedLatest),
            TopFeedback(selected, selectedLatest));
    }

    private static List<FeedbackHighlight> TopFeedback(List<Repository> selected, List<Analysis> latest)
    {
        var names = selected.ToDictionary(r => r.Id, r => r.Name);
        return
        [
            .. latest
                .SelectMany(a => a.Feedback.Where(f => !f.IsStrength).Select(f => (a.RepositoryId, Feedback: f)))
                .OrderByDescending(x => x.Feedback.Severity)
                .ThenBy(x => names[x.RepositoryId], StringComparer.OrdinalIgnoreCase)
                .Take(TopFeedbackCount)
                .Select(x => new FeedbackHighlight(x.RepositoryId, names[x.RepositoryId], x.Feedback)),
        ];
    }
}
