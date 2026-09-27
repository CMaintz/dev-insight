using DevInsight.Domain.Analyses.Engine;
using static DevInsight.Domain.Analyses.Rules.AnalysisMetricExtensions;

namespace DevInsight.Domain.Analyses.Rules;

/// <summary>Always yields one structure finding when there is code: a monolith, large files, or a strength.</summary>
public sealed class StructureRule : IFeedbackRule
{
    public IEnumerable<FeedbackFinding> Evaluate(Analysis analysis)
    {
        if (analysis.Metric(MetricKeys.SourceFileCount) == 0)
        {
            yield break;
        }

        var largeFiles = (int)analysis.Metric(MetricKeys.FilesOver500Lines);
        var largest = analysis.LargestFiles.FirstOrDefault();
        if (analysis.Metric(MetricKeys.MonolithIndicator) > 0)
        {
            yield return new FeedbackFinding("monolith", MetricCategory.Structure, Severity.High,
                "Structure suggests a monolith",
                $"Most of the code lives in {largeFiles} file(s) over {StructureAnalyzer.LargeFileLines} lines"
                + (largest is null ? "." : $", the largest being {largest.Path} ({largest.Lines} lines).")
                + " Consider separating responsibilities into smaller modules.");
        }
        else if (largeFiles > 0)
        {
            yield return new FeedbackFinding("large-files", MetricCategory.Structure, Severity.Medium,
                $"{largeFiles} file(s) exceed {StructureAnalyzer.LargeFileLines} lines",
                (largest is null ? string.Empty : $"Start with {largest.Path} ({largest.Lines} lines). ")
                + "Large files usually carry several responsibilities; splitting them by concern makes them easier to test.");
        }
        else
        {
            yield return new FeedbackFinding("separation-of-concerns", MetricCategory.Structure, Severity.Low,
                "Good separation of concerns",
                $"No file exceeds {StructureAnalyzer.LargeFileLines} lines and the median file is {analysis.Metric(MetricKeys.MedianFileLines):0} lines — responsibilities are split into focused units.",
                IsStrength: true);
        }
    }
}

public sealed class TestsRule : IFeedbackRule
{
    public IEnumerable<FeedbackFinding> Evaluate(Analysis analysis)
    {
        if (analysis.Metric(MetricKeys.SourceFileCount) == 0)
        {
            yield break;
        }

        var ratio = analysis.Metric(MetricKeys.TestFileRatio);
        if (analysis.Metric(MetricKeys.HasTests) == 0)
        {
            yield return new FeedbackFinding("missing-tests", MetricCategory.Quality, Severity.High,
                "No tests found",
                "Add tests for the central modules first — the code with the most logic and the most callers.");
        }
        else if (ratio < 0.1)
        {
            yield return new FeedbackFinding("thin-tests", MetricCategory.Quality, Severity.Medium,
                "Test coverage looks thin",
                $"Only {Percent(ratio)} of source files are tests. Central modules are likely missing tests.");
        }
        else if (ratio >= 0.2)
        {
            yield return new FeedbackFinding("well-tested", MetricCategory.Quality, Severity.Low,
                "Well-tested codebase", $"{Percent(ratio)} of source files are tests.", IsStrength: true);
        }
    }
}

public sealed class ProjectHygieneRule : IFeedbackRule
{
    public IEnumerable<FeedbackFinding> Evaluate(Analysis analysis)
    {
        if (analysis.Metric(MetricKeys.HasReadme) == 0)
        {
            yield return new FeedbackFinding("missing-readme", MetricCategory.Quality, Severity.Medium,
                "No README",
                "A README that explains why the project exists, how to run it and what decisions shaped it is the first thing a reviewer reads.");
        }

        if (analysis.Metric(MetricKeys.HasLintConfig) == 0)
        {
            yield return new FeedbackFinding("missing-lint", MetricCategory.Quality, Severity.Low,
                "No linter or formatter configuration",
                "A committed lint/format config (e.g. .editorconfig, eslint.config.js) keeps style consistent without review effort.");
        }

        if (analysis.Metric(MetricKeys.HasCi) == 0)
        {
            yield return new FeedbackFinding("missing-ci", MetricCategory.Quality, Severity.Low,
                "No continuous integration",
                "A CI workflow that builds and tests every push catches regressions before they are merged.");
        }
    }
}
