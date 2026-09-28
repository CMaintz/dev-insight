namespace DevInsight.Domain.Analyses.Engine;

/// <summary>
/// Commit-level behaviour: message quality and commit size. Merge commits are excluded — their
/// message and size are generated, not authored. The message-quality metric feeds the quality
/// score; the rest are informational and drive feedback.
/// </summary>
public static class CommitQualityAnalyzer
{
    public const int LargeCommitLines = 1000;
    private const MetricCategory Category = MetricCategory.CommitQuality;

    /// <summary>Commit-size buckets by lines changed: [Min, Max).</summary>
    private static readonly (string Key, int Min, int Max)[] SizeBuckets =
    [
        (MetricKeys.CommitsSizeXs, 0, 10),
        (MetricKeys.CommitsSizeS, 10, 50),
        (MetricKeys.CommitsSizeM, 50, 250),
        (MetricKeys.CommitsSizeL, 250, LargeCommitLines),
        (MetricKeys.CommitsSizeXl, LargeCommitLines, int.MaxValue),
    ];

    public static IReadOnlyList<AnalysisMetric> Analyze(IReadOnlyList<CommitRecord> commits)
    {
        var authored = Authored(commits);
        return authored.Count == 0
            ? [AnalysisMetric.Info(MetricKeys.VagueCommitCount, Category, 0)]
            : [.. MessageMetrics(authored), .. SizeMetrics(authored)];
    }

    /// <summary>Average 0–100 message score of authored (non-merge) commits; 0 when there are none.</summary>
    public static double MessageQuality(IReadOnlyList<CommitRecord> commits)
    {
        var authored = Authored(commits);
        return authored.Count == 0 ? 0 : authored.Average(c => CommitMessageClassifier.Score(c.Subject));
    }

    private static List<CommitRecord> Authored(IReadOnlyList<CommitRecord> commits) => [.. commits.Where(c => !c.IsMerge)];

    private static IEnumerable<AnalysisMetric> MessageMetrics(List<CommitRecord> authored)
    {
        var vagueCount = authored.Count(c => CommitMessageClassifier.IsVague(c.Subject));
        yield return AnalysisMetric.Info(MetricKeys.VagueCommitCount, Category, vagueCount);
        yield return AnalysisMetric.Info(MetricKeys.VagueCommitRatio, Category, vagueCount / (double)authored.Count);
    }

    private static IEnumerable<AnalysisMetric> SizeMetrics(List<CommitRecord> authored)
    {
        var sizes = authored.Select(c => (double)c.LinesChanged).ToList();
        var mean = sizes.Average();
        yield return AnalysisMetric.Info(MetricKeys.AverageCommitSize, Category, mean);
        yield return AnalysisMetric.Info(MetricKeys.CommitSizeStdDev, Category, Math.Sqrt(sizes.Average(s => (s - mean) * (s - mean))));
        yield return AnalysisMetric.Info(MetricKeys.LargeCommitRatio, Category,
            authored.Count(c => c.LinesChanged >= LargeCommitLines) / (double)authored.Count);
        foreach (var (key, min, max) in SizeBuckets)
        {
            yield return AnalysisMetric.Info(key, Category, authored.Count(c => c.LinesChanged >= min && c.LinesChanged < max));
        }
    }
}
