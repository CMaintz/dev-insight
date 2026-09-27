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

    public static IReadOnlyList<AnalysisMetric> Analyze(IReadOnlyList<CommitRecord> commits, DateTimeOffset now)
    {
        const MetricCategory category = MetricCategory.Activity;
        if (commits.Count == 0)
        {
            return
            [
                AnalysisMetric.Info(MetricKeys.TotalCommits, category, 0),
                AnalysisMetric.Scored(MetricKeys.DaysSinceLastCommit, category, 0, 0, 0.4),
                AnalysisMetric.Scored(MetricKeys.CommitsPerWeekRecent, category, 0, 0, 0.3),
                AnalysisMetric.Scored(MetricKeys.ActiveWeeksRatio, category, 0, 0, 0.3),
            ];
        }

        var last = commits.Max(c => c.AuthoredAt);
        var first = commits.Min(c => c.AuthoredAt);
        var daysSinceLast = Math.Max(0, (now - last).TotalDays);
        var recentCommits = commits.Count(c => c.AuthoredAt >= now.AddDays(-7 * RecentWeeks));
        var perWeekRecent = recentCommits / (double)RecentWeeks;
        var lifetimeWeeks = Math.Max(1, (now - first).TotalDays / 7);
        var activeWeeks = commits
            .Where(c => c.AuthoredAt >= now.AddDays(-7 * ConsistencyWeeks))
            .Select(c => Weeks.StartOf(c.AuthoredAt))
            .Distinct()
            .Count();
        var activeRatio = Math.Min(1, activeWeeks / (double)ConsistencyWeeks);

        return
        [
            AnalysisMetric.Info(MetricKeys.TotalCommits, category, commits.Count),
            AnalysisMetric.Scored(MetricKeys.DaysSinceLastCommit, category, daysSinceLast,
                Scoring.LinearDecay(daysSinceLast, best: 7, worst: 365), 0.4),
            AnalysisMetric.Scored(MetricKeys.CommitsPerWeekRecent, category, perWeekRecent,
                100 * perWeekRecent / TargetCommitsPerWeek, 0.3),
            AnalysisMetric.Scored(MetricKeys.ActiveWeeksRatio, category, activeRatio, 100 * activeRatio, 0.3),
            AnalysisMetric.Info(MetricKeys.CommitsPerWeekLifetime, category, commits.Count / lifetimeWeeks),
            AnalysisMetric.Info(MetricKeys.IsDormant, category, daysSinceLast > DormantAfterDays ? 1 : 0),
        ];
    }
}
