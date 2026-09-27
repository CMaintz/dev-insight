namespace DevInsight.Domain.Analyses.Rules;

/// <summary>A deterministic heuristic that turns an analysis's metrics into feedback.</summary>
public interface IFeedbackRule
{
    IEnumerable<FeedbackFinding> Evaluate(Analysis analysis);
}

internal static class AnalysisMetricExtensions
{
    public static double Metric(this Analysis analysis, string name) => analysis.MetricValue(name) ?? 0;

    public static bool HasCommits(this Analysis analysis) => analysis.Metric(MetricKeys.TotalCommits) > 0;

    public static string Percent(double ratio) => $"{Math.Round(ratio * 100):0}%";
}
