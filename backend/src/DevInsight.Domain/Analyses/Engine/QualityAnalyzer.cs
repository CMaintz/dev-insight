namespace DevInsight.Domain.Analyses.Engine;

/// <summary>
/// Quality = 30% tests + 25% commit-message quality + 15% README + 15% lint/format config + 15% CI.
/// Tests: none = 0; any = 60, plus up to 40 as test files approach 20% of source files.
/// </summary>
public static class QualityAnalyzer
{
    private const double IdealTestFileRatio = 0.2;

    /// <param name="scopedFiles">Files in scope, used for test presence.</param>
    /// <param name="allFiles">All files at HEAD, used for project hygiene (README, lint, CI).</param>
    public static IReadOnlyList<AnalysisMetric> Analyze(
        IReadOnlyList<SourceFile> scopedFiles,
        IReadOnlyList<SourceFile> allFiles,
        double commitMessageQuality,
        int contributorCount)
    {
        const MetricCategory category = MetricCategory.Quality;
        var sourceCount = scopedFiles.Count(f => FileClassifier.IsSource(f.Path));
        var testCount = scopedFiles.Count(f => FileClassifier.IsTest(f.Path));
        var testRatio = sourceCount == 0 ? 0 : testCount / (double)sourceCount;
        var testPoints = testCount == 0 ? 0 : 60 + Math.Min(40, 40 * testRatio / IdealTestFileRatio);
        var hasReadme = allFiles.Any(f => FileClassifier.IsReadme(f.Path));
        var hasLint = allFiles.Any(f => FileClassifier.IsLintConfig(f.Path));
        var hasCi = allFiles.Any(f => FileClassifier.IsCiConfig(f.Path));

        return
        [
            AnalysisMetric.Scored(MetricKeys.HasTests, category, testCount > 0 ? 1 : 0, testPoints, 0.30),
            AnalysisMetric.Info(MetricKeys.TestFileRatio, category, testRatio),
            AnalysisMetric.Scored(MetricKeys.CommitMessageQuality, category, commitMessageQuality, commitMessageQuality, 0.25),
            AnalysisMetric.Scored(MetricKeys.HasReadme, category, Flag(hasReadme), 100 * Flag(hasReadme), 0.15),
            AnalysisMetric.Scored(MetricKeys.HasLintConfig, category, Flag(hasLint), 100 * Flag(hasLint), 0.15),
            AnalysisMetric.Scored(MetricKeys.HasCi, category, Flag(hasCi), 100 * Flag(hasCi), 0.15),
            AnalysisMetric.Info(MetricKeys.HasLicense, category, Flag(allFiles.Any(f => FileClassifier.IsLicense(f.Path)))),
            AnalysisMetric.Info(MetricKeys.ContributorCount, category, contributorCount),
        ];
    }

    private static int Flag(bool value) => value ? 1 : 0;
}
