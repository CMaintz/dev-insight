namespace DevInsight.Domain.Analyses;

/// <summary>Score arithmetic shared by the analysers. All scores are integers in [0, 100].</summary>
public static class Scoring
{
    public const double ActivityWeight = 0.30;
    public const double StructureWeight = 0.30;
    public const double QualityWeight = 0.40;

    public static int Clamp(double points) =>
        (int)Math.Round(Math.Clamp(points, 0, 100), MidpointRounding.AwayFromZero);

    public static int Overall(int activity, int structure, int quality) =>
        Clamp((activity * ActivityWeight) + (structure * StructureWeight) + (quality * QualityWeight));

    /// <summary>Weighted sum of the scored metrics' points.</summary>
    public static int Dimension(IEnumerable<AnalysisMetric> metrics) =>
        Clamp(metrics.Where(m => m.IncludedInScore).Sum(m => (m.Points ?? 0) * (m.Weight ?? 0)));

    /// <summary>100 at or below <paramref name="best"/>, 0 at or above <paramref name="worst"/>, linear between.</summary>
    public static double LinearDecay(double value, double best, double worst)
    {
        if (value <= best)
        {
            return 100;
        }

        return value >= worst ? 0 : 100 * (1 - ((value - best) / (worst - best)));
    }
}
