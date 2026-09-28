namespace DevInsight.Domain.Analyses.Engine;

/// <summary>
/// Structure = 35% large-file share + 25% monolith check + 20% median file size + 20% folder depth.
/// Only source files count; vendored and generated code is ignored.
/// </summary>
public static class StructureAnalyzer
{
    public const int LargeFileLines = 500;

    /// <summary>A codebase is flagged as a monolith when over half its code lives in large files.</summary>
    private const double MonolithLargeFileLocShare = 0.5;
    private const MetricCategory Category = MetricCategory.Structure;

    public static IReadOnlyList<AnalysisMetric> Analyze(IReadOnlyList<SourceFile> files)
    {
        var source = files.Where(f => FileClassifier.IsSource(f.Path)).ToList();
        return source.Count == 0 ? NoSource() : Metrics(StructureFacts.Of(source));
    }

    private static IReadOnlyList<AnalysisMetric> Metrics(StructureFacts facts) =>
    [
        AnalysisMetric.Info(MetricKeys.SourceFileCount, Category, facts.FileCount),
        AnalysisMetric.Info(MetricKeys.TotalLinesOfCode, Category, facts.TotalLines),
        AnalysisMetric.Info(MetricKeys.LargestFileLines, Category, facts.LargestFileLines),
        AnalysisMetric.Info(MetricKeys.FilesOver500Lines, Category, facts.LargeFileCount),
        AnalysisMetric.Info(MetricKeys.MaxFolderDepth, Category, facts.MaxDepth),
        // 25% of files being large already costs every point.
        AnalysisMetric.Scored(MetricKeys.LargeFileRatio, Category, facts.LargeRatio, 100 - (facts.LargeRatio * 400), 0.35),
        AnalysisMetric.Scored(MetricKeys.MonolithIndicator, Category, facts.IsMonolith ? 1 : 0, facts.IsMonolith ? 0 : 100, 0.25),
        AnalysisMetric.Scored(MetricKeys.MedianFileLines, Category, facts.MedianLines,
            Scoring.LinearDecay(facts.MedianLines, best: 200, worst: 800), 0.20),
        AnalysisMetric.Scored(MetricKeys.AverageFolderDepth, Category, facts.AverageDepth, DepthPoints(facts.AverageDepth), 0.20),
    ];

    private static IReadOnlyList<AnalysisMetric> NoSource() =>
    [
        AnalysisMetric.Info(MetricKeys.SourceFileCount, Category, 0),
        AnalysisMetric.Scored(MetricKeys.LargeFileRatio, Category, 0, 0, 0.35),
        AnalysisMetric.Scored(MetricKeys.MonolithIndicator, Category, 0, 0, 0.25),
        AnalysisMetric.Scored(MetricKeys.MedianFileLines, Category, 0, 0, 0.20),
        AnalysisMetric.Scored(MetricKeys.AverageFolderDepth, Category, 0, 0, 0.20),
    ];

    /// <summary>Everything in the root folder is flat; very deep nesting is hard to navigate. 1.5–5 is ideal.</summary>
    private static double DepthPoints(double averageDepth) => averageDepth switch
    {
        < 1.5 => 100 - ((1.5 - averageDepth) * 60),
        > 5 => 100 - ((averageDepth - 5) * 20),
        _ => 100,
    };

    /// <summary>The measurements behind the structure metrics, for a non-empty list of source files.</summary>
    private sealed record StructureFacts(
        int FileCount, long TotalLines, int LargestFileLines, int LargeFileCount, bool IsMonolith,
        double MedianLines, double AverageDepth, int MaxDepth)
    {
        public double LargeRatio => LargeFileCount / (double)FileCount;

        public static StructureFacts Of(IReadOnlyList<SourceFile> source)
        {
            var totalLines = source.Sum(f => (long)f.Lines);
            var large = source.Where(f => f.Lines > LargeFileLines).ToList();
            var largeLocShare = totalLines == 0 ? 0 : large.Sum(f => (long)f.Lines) / (double)totalLines;
            return new StructureFacts(
                source.Count, totalLines, source.Max(f => f.Lines), large.Count, largeLocShare > MonolithLargeFileLocShare,
                Median(source.Select(f => f.Lines)), source.Average(f => FileClassifier.Depth(f.Path)),
                source.Max(f => FileClassifier.Depth(f.Path)));
        }

        private static double Median(IEnumerable<int> values)
        {
            var sorted = values.Order().ToList();
            var mid = sorted.Count / 2;
            return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
        }
    }
}
