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

    public static IReadOnlyList<AnalysisMetric> Analyze(IReadOnlyList<SourceFile> files)
    {
        const MetricCategory category = MetricCategory.Structure;
        var source = files.Where(f => FileClassifier.IsSource(f.Path)).ToList();
        if (source.Count == 0)
        {
            return
            [
                AnalysisMetric.Info(MetricKeys.SourceFileCount, category, 0),
                AnalysisMetric.Scored(MetricKeys.LargeFileRatio, category, 0, 0, 0.35),
                AnalysisMetric.Scored(MetricKeys.MonolithIndicator, category, 0, 0, 0.25),
                AnalysisMetric.Scored(MetricKeys.MedianFileLines, category, 0, 0, 0.20),
                AnalysisMetric.Scored(MetricKeys.AverageFolderDepth, category, 0, 0, 0.20),
            ];
        }

        var totalLines = source.Sum(f => (long)f.Lines);
        var large = source.Where(f => f.Lines > LargeFileLines).ToList();
        var largeRatio = large.Count / (double)source.Count;
        var largeLocShare = totalLines == 0 ? 0 : large.Sum(f => (long)f.Lines) / (double)totalLines;
        var isMonolith = largeLocShare > MonolithLargeFileLocShare;
        var median = Median(source.Select(f => f.Lines));
        var averageDepth = source.Average(f => FileClassifier.Depth(f.Path));

        return
        [
            AnalysisMetric.Info(MetricKeys.SourceFileCount, category, source.Count),
            AnalysisMetric.Info(MetricKeys.TotalLinesOfCode, category, totalLines),
            AnalysisMetric.Info(MetricKeys.LargestFileLines, category, source.Max(f => f.Lines)),
            AnalysisMetric.Info(MetricKeys.FilesOver500Lines, category, large.Count),
            AnalysisMetric.Info(MetricKeys.MaxFolderDepth, category, source.Max(f => FileClassifier.Depth(f.Path))),
            // 25% of files being large already costs every point.
            AnalysisMetric.Scored(MetricKeys.LargeFileRatio, category, largeRatio, 100 - (largeRatio * 400), 0.35),
            AnalysisMetric.Scored(MetricKeys.MonolithIndicator, category, isMonolith ? 1 : 0, isMonolith ? 0 : 100, 0.25),
            AnalysisMetric.Scored(MetricKeys.MedianFileLines, category, median,
                Scoring.LinearDecay(median, best: 200, worst: 800), 0.20),
            AnalysisMetric.Scored(MetricKeys.AverageFolderDepth, category, averageDepth, DepthPoints(averageDepth), 0.20),
        ];
    }

    /// <summary>Everything in the root folder is flat; very deep nesting is hard to navigate. 1.5–5 is ideal.</summary>
    private static double DepthPoints(double averageDepth) => averageDepth switch
    {
        < 1.5 => 100 - ((1.5 - averageDepth) * 60),
        > 5 => 100 - ((averageDepth - 5) * 20),
        _ => 100,
    };

    private static double Median(IEnumerable<int> values)
    {
        var sorted = values.Order().ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }
}
