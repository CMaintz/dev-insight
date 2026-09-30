namespace DevInsight.Domain.Analyses.Engine;

/// <summary>
/// Activity = 40% recency + 30% recent frequency + 30% consistency.
/// Recency: 100 if the last commit is ≤ 7 days old, falling linearly to 0 at 365 days.
/// Frequency: commits/week over the last 12 weeks, 3+/week = 100.
/// Consistency: share of the last 26 weeks with at least one commit.
/// </summary>
public static class ActivityAnalyzer
{
    public const int DormantAfterDays = 180;
    private const int RecentWeeks = 12;
    private const int ConsistencyWeeks = 26;
    private const double TargetCommitsPerWeek = 3;
    private const MetricCategory Category = MetricCategory.Activity;

    public static IReadOnlyList<AnalysisMetric> Analyze(IReadOnlyList<CommitRecord> commits, DateTimeOffset now) =>
        commits.Count == 0 ? NoActivity() : Metrics(ActivityFacts.Of(commits, now));

    private static IReadOnlyList<AnalysisMetric> Metrics(ActivityFacts facts) =>
    [
        AnalysisMetric.Info(MetricKeys.TotalCommits, Category, facts.TotalCommits),
        AnalysisMetric.Scored(MetricKeys.DaysSinceLastCommit, Category, facts.DaysSinceLast,
            Scoring.LinearDecay(facts.DaysSinceLast, best: 7, worst: 365), 0.4),
        AnalysisMetric.Scored(MetricKeys.CommitsPerWeekRecent, Category, facts.PerWeekRecent,
            100 * facts.PerWeekRecent / TargetCommitsPerWeek, 0.3),
        AnalysisMetric.Scored(MetricKeys.ActiveWeeksRatio, Category, facts.ActiveRatio, 100 * facts.ActiveRatio, 0.3),
        AnalysisMetric.Info(MetricKeys.CommitsPerWeekLifetime, Category, facts.PerWeekLifetime),
        AnalysisMetric.Info(MetricKeys.IsDormant, Category, facts.DaysSinceLast > DormantAfterDays ? 1 : 0),
    ];

    private static IReadOnlyList<AnalysisMetric> NoActivity() =>
    [
        AnalysisMetric.Info(MetricKeys.TotalCommits, Category, 0),
        AnalysisMetric.Scored(MetricKeys.DaysSinceLastCommit, Category, 0, 0, 0.4),
        AnalysisMetric.Scored(MetricKeys.CommitsPerWeekRecent, Category, 0, 0, 0.3),
        AnalysisMetric.Scored(MetricKeys.ActiveWeeksRatio, Category, 0, 0, 0.3),
    ];

    /// <summary>The measurements behind the activity metrics, for a non-empty commit list.</summary>
    private sealed record ActivityFacts(int TotalCommits, double DaysSinceLast, double PerWeekRecent, double PerWeekLifetime, double ActiveRatio)
    {
        public static ActivityFacts Of(IReadOnlyList<CommitRecord> commits, DateTimeOffset now)
        {
            var recentCommits = commits.Count(c => c.AuthoredAt >= now.AddDays(-7 * RecentWeeks));
            var lifetimeWeeks = Math.Max(1, (now - commits.Min(c => c.AuthoredAt)).TotalDays / 7);
            return new ActivityFacts(
                commits.Count,
                Math.Max(0, (now - commits.Max(c => c.AuthoredAt)).TotalDays),
                recentCommits / (double)RecentWeeks,
                commits.Count / lifetimeWeeks,
                Math.Min(1, ActiveWeeks(commits, now) / (double)ConsistencyWeeks));
        }

        private static int ActiveWeeks(IReadOnlyList<CommitRecord> commits, DateTimeOffset now) =>
            commits
                .Where(c => c.AuthoredAt >= now.AddDays(-7 * ConsistencyWeeks))
                .Select(c => Weeks.StartOf(c.AuthoredAt))
                .Distinct()
                .Count();
    }
}
