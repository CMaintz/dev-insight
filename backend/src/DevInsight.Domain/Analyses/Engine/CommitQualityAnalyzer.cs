namespace DevInsight.Domain.Analyses.Engine;

/// <summary>
/// Commit-level behaviour: message quality and commit size. Merge commits are excluded — their
/// message and size are generated, not authored. The message-quality metric feeds the quality
/// score; the rest are informational and drive feedback.
/// </summary>
public static class CommitQualityAnalyzer
{
    public const int LargeCommitLines = 1000;

    public static IReadOnlyList<AnalysisMetric> Analyze(IReadOnlyList<CommitRecord> commits)
    {
        const MetricCategory category = MetricCategory.CommitQuality;
        var authored = commits.Where(c => !c.IsMerge).ToList();
        if (authored.Count == 0)
        {
            return [AnalysisMetric.Info(MetricKeys.VagueCommitCount, category, 0)];
        }

        var vagueCount = authored.Count(c => CommitMessageClassifier.IsVague(c.Subject));
        var sizes = authored.Select(c => (double)c.LinesChanged).ToList();
        var mean = sizes.Average();
        var stdDev = Math.Sqrt(sizes.Average(s => (s - mean) * (s - mean)));

        return
        [
            AnalysisMetric.Info(MetricKeys.VagueCommitCount, category, vagueCount),
            AnalysisMetric.Info(MetricKeys.VagueCommitRatio, category, vagueCount / (double)authored.Count),
            AnalysisMetric.Info(MetricKeys.AverageCommitSize, category, mean),
            AnalysisMetric.Info(MetricKeys.CommitSizeStdDev, category, stdDev),
            AnalysisMetric.Info(MetricKeys.LargeCommitRatio, category,
                authored.Count(c => c.LinesChanged >= LargeCommitLines) / (double)authored.Count),
            AnalysisMetric.Info(MetricKeys.CommitsSizeXs, category, authored.Count(c => c.LinesChanged < 10)),
            AnalysisMetric.Info(MetricKeys.CommitsSizeS, category, authored.Count(c => c.LinesChanged is >= 10 and < 50)),
            AnalysisMetric.Info(MetricKeys.CommitsSizeM, category, authored.Count(c => c.LinesChanged is >= 50 and < 250)),
            AnalysisMetric.Info(MetricKeys.CommitsSizeL, category,
                authored.Count(c => c.LinesChanged is >= 250 and < LargeCommitLines)),
            AnalysisMetric.Info(MetricKeys.CommitsSizeXl, category, authored.Count(c => c.LinesChanged >= LargeCommitLines)),
        ];
    }

    /// <summary>Average 0–100 message score of authored (non-merge) commits; 0 when there are none.</summary>
    public static double MessageQuality(IReadOnlyList<CommitRecord> commits)
    {
        var authored = commits.Where(c => !c.IsMerge).ToList();
        return authored.Count == 0 ? 0 : authored.Average(c => CommitMessageClassifier.Score(c.Subject));
    }
}
