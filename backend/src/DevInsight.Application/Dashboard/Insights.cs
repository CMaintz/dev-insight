using DevInsight.Application.Abstractions;
using DevInsight.Domain.Analyses;
using DevInsight.Domain.Repositories;

namespace DevInsight.Application.Dashboard;

public sealed record LanguageShare(string Language, long Bytes, double Share);

public sealed record ScorePoint(DateOnly Date, int Overall, int Activity, int Structure, int Quality);

public sealed record Scores(int Overall, int Activity, int Structure, int Quality);

public sealed record CommitSizeDistribution(int Xs, int S, int M, int L, int Xl);

public sealed record CommitQualitySummary(int Commits, int VagueCommits, CommitSizeDistribution Sizes);

/// <summary>Aggregations shared by the private dashboard and the public portfolio.</summary>
public static class Insights
{
    public static IReadOnlyList<LanguageShare> Languages(IEnumerable<Repository> repositories)
    {
        var totals = repositories
            .SelectMany(r => r.Languages)
            .GroupBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Language: g.Key, Bytes: g.Sum(kv => kv.Value)))
            .Where(x => x.Bytes > 0)
            .OrderByDescending(x => x.Bytes)
            .ToList();
        var all = totals.Sum(x => x.Bytes);
        return [.. totals.Select(x => new LanguageShare(x.Language, x.Bytes, Math.Round(x.Bytes / (double)all, 4)))];
    }

    /// <summary>Unweighted mean of the latest analyses — every selected repository counts equally.</summary>
    public static Scores? AverageScores(IReadOnlyCollection<Analysis> latest) =>
        latest.Count == 0
            ? null
            : new Scores(
                Avg(latest.Select(a => a.OverallScore)),
                Avg(latest.Select(a => a.ActivityScore)),
                Avg(latest.Select(a => a.StructureScore)),
                Avg(latest.Select(a => a.QualityScore)));

    /// <summary>Weekly activity summed across repositories.</summary>
    public static IReadOnlyList<ActivityWeek> CombinedTimeline(IEnumerable<Analysis> latest) =>
    [
        .. latest
            .SelectMany(a => a.Timeline)
            .GroupBy(w => w.WeekStart)
            .OrderBy(g => g.Key)
            .Select(g => new ActivityWeek(g.Key, g.Sum(w => w.Commits), g.Sum(w => w.Additions), g.Sum(w => w.Deletions))),
    ];

    /// <summary>
    /// Score evolution: for every day an analysis happened, the average of each repository's most
    /// recent score as of that day. Repositories join the average from their first analysis.
    /// </summary>
    public static IReadOnlyList<ScorePoint> ScoreEvolution(IReadOnlyList<ScoreSnapshot> history)
    {
        var current = new Dictionary<Guid, ScoreSnapshot>();
        var points = new List<ScorePoint>();
        foreach (var day in history.OrderBy(s => s.CreatedAt).GroupBy(s => DateOnly.FromDateTime(s.CreatedAt.UtcDateTime)))
        {
            foreach (var snapshot in day)
            {
                current[snapshot.RepositoryId] = snapshot;
            }

            var values = current.Values;
            points.Add(new ScorePoint(
                day.Key,
                Avg(values.Select(v => v.OverallScore)),
                Avg(values.Select(v => v.ActivityScore)),
                Avg(values.Select(v => v.StructureScore)),
                Avg(values.Select(v => v.QualityScore))));
        }

        return points;
    }

    public static CommitQualitySummary CommitQuality(IEnumerable<Analysis> latest)
    {
        var list = latest.ToList();
        int Sum(string key) => (int)list.Sum(a => a.MetricValue(key) ?? 0);
        var sizes = new CommitSizeDistribution(
            Sum(MetricKeys.CommitsSizeXs),
            Sum(MetricKeys.CommitsSizeS),
            Sum(MetricKeys.CommitsSizeM),
            Sum(MetricKeys.CommitsSizeL),
            Sum(MetricKeys.CommitsSizeXl));
        return new CommitQualitySummary(Sum(MetricKeys.TotalCommits), Sum(MetricKeys.VagueCommitCount), sizes);
    }

    private static int Avg(IEnumerable<int> values) => (int)Math.Round(values.Average(), MidpointRounding.AwayFromZero);
}
