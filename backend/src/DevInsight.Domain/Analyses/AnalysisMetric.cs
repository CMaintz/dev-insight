using DevInsight.Domain.Common;

namespace DevInsight.Domain.Analyses;

/// <summary>
/// One measured value. <see cref="Points"/> is the 0–100 rating of this metric and
/// <see cref="Weight"/> its share of the dimension score, so every score can be traced back
/// to the metrics behind it. Informational metrics are not included in any score.
/// </summary>
public sealed class AnalysisMetric
{
    private AnalysisMetric() { }

    public Guid Id { get; private set; }
    public Guid AnalysisId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public MetricCategory Category { get; private set; }
    public double Value { get; private set; }
    public bool IncludedInScore { get; private set; }
    public int? Points { get; private set; }
    public double? Weight { get; private set; }

    public static AnalysisMetric Scored(string name, MetricCategory category, double value, double points, double weight) =>
        new()
        {
            Id = Ids.New(),
            Name = name,
            Category = category,
            Value = Round(value),
            IncludedInScore = true,
            Points = Scoring.Clamp(points),
            Weight = weight,
        };

    public static AnalysisMetric Info(string name, MetricCategory category, double value) =>
        new() { Id = Ids.New(), Name = name, Category = category, Value = Round(value) };

    private static double Round(double value) => Math.Round(value, 3);
}
