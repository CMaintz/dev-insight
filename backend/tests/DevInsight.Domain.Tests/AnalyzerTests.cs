using DevInsight.Domain.Analyses;
using DevInsight.Domain.Analyses.Engine;
using static DevInsight.Domain.Tests.TestData;

namespace DevInsight.Domain.Tests;

public class ActivityAnalyzerTests
{
    [Fact]
    public void No_commits_scores_zero()
    {
        var metrics = ActivityAnalyzer.Analyze([], Now);
        Scoring.Dimension(metrics).ShouldBe(0);
    }

    [Fact]
    public void Three_commits_a_week_every_week_scores_full()
    {
        var commits = Enumerable.Range(0, 26 * 3).Select(i => Commit(daysAgo: 1 + (i * 7 / 3))).ToList();
        Scoring.Dimension(ActivityAnalyzer.Analyze(commits, Now)).ShouldBe(100);
    }

    [Fact]
    public void Recency_decays_linearly_after_a_week()
    {
        var metrics = ActivityAnalyzer.Analyze([Commit(daysAgo: 186)], Now);
        var recency = metrics.Single(m => m.Name == MetricKeys.DaysSinceLastCommit);
        recency.Value.ShouldBe(186);
        recency.Points.ShouldBe(50); // halfway between 7 and 365 days
    }

    [Fact]
    public void Project_quiet_for_over_half_a_year_is_dormant()
    {
        var metrics = ActivityAnalyzer.Analyze([Commit(daysAgo: 200)], Now);
        metrics.Single(m => m.Name == MetricKeys.IsDormant).Value.ShouldBe(1);
    }

    [Fact]
    public void Weights_of_scored_metrics_sum_to_one() =>
        ActivityAnalyzer.Analyze(WeeklyCommits(4), Now).Where(m => m.IncludedInScore).Sum(m => m.Weight!.Value).ShouldBe(1, 1e-9);
}

public class CommitQualityAnalyzerTests
{
    [Fact]
    public void Merge_commits_are_excluded()
    {
        var commits = new[] { Commit(1, "Merge pull request #1 from x/y", isMerge: true, additions: 5000), Commit(2, "fix") };
        var metrics = CommitQualityAnalyzer.Analyze(commits);
        metrics.Single(m => m.Name == MetricKeys.VagueCommitCount).Value.ShouldBe(1);
        metrics.Single(m => m.Name == MetricKeys.AverageCommitSize).Value.ShouldBe(25);
    }

    [Fact]
    public void Commit_sizes_are_bucketed()
    {
        var commits = new[] { Commit(1, additions: 5, deletions: 0), Commit(2, additions: 30, deletions: 0), Commit(3, additions: 2000, deletions: 0) };
        var metrics = CommitQualityAnalyzer.Analyze(commits).ToDictionary(m => m.Name, m => m.Value);
        (metrics[MetricKeys.CommitsSizeXs], metrics[MetricKeys.CommitsSizeS], metrics[MetricKeys.CommitsSizeXl]).ShouldBe((1, 1, 1));
        metrics[MetricKeys.LargeCommitRatio].ShouldBe(0.333);
    }

    [Fact]
    public void Message_quality_averages_authored_commits() =>
        CommitQualityAnalyzer.MessageQuality([Commit(1, "fix"), Commit(2, "feat(api): add portfolio endpoint")]).ShouldBe(50);
}

public class StructureAnalyzerTests
{
    [Fact]
    public void Healthy_structure_scores_full() =>
        Scoring.Dimension(StructureAnalyzer.Analyze(HealthyFiles())).ShouldBe(100);

    [Fact]
    public void Code_concentrated_in_large_files_is_a_monolith()
    {
        var files = new[] { File("src/Program.cs", 2400), File("src/Helpers.cs", 120), File("src/Models.cs", 90) };
        var metrics = StructureAnalyzer.Analyze(files).ToDictionary(m => m.Name);
        metrics[MetricKeys.MonolithIndicator].Value.ShouldBe(1);
        metrics[MetricKeys.FilesOver500Lines].Value.ShouldBe(1);
        metrics[MetricKeys.LargestFileLines].Value.ShouldBe(2400);
    }

    [Fact]
    public void Flat_repository_loses_depth_points()
    {
        var files = new[] { File("a.py", 50), File("b.py", 50) };
        StructureAnalyzer.Analyze(files).Single(m => m.Name == MetricKeys.AverageFolderDepth).Points.ShouldBe(10);
    }

    [Fact]
    public void Non_source_files_are_ignored()
    {
        var metrics = StructureAnalyzer.Analyze([File("README.md", 5000), File("data.json", 9000)]);
        metrics.Single(m => m.Name == MetricKeys.SourceFileCount).Value.ShouldBe(0);
        Scoring.Dimension(metrics).ShouldBe(0);
    }
}

public class QualityAnalyzerTests
{
    [Fact]
    public void Healthy_project_with_descriptive_commits_scores_full() =>
        Scoring.Dimension(QualityAnalyzer.Analyze(HealthyFiles(), HealthyFiles(), 100, 1)).ShouldBe(100);

    [Fact]
    public void Without_project_hygiene_only_commit_quality_scores()
    {
        var files = new[] { File("main.py", 100) };
        Scoring.Dimension(QualityAnalyzer.Analyze(files, files, 100, 1)).ShouldBe(25);
    }

    [Fact]
    public void A_few_tests_earn_partial_points()
    {
        var files = Enumerable.Range(0, 19).Select(i => File($"src/f{i}.ts", 50)).Append(File("src/f.spec.ts", 50)).ToList();
        // 1 test in 20 source files: 60 + 40 * (0.05 / 0.2) = 70
        QualityAnalyzer.Analyze(files, files, 0, 1).Single(m => m.Name == MetricKeys.HasTests).Points.ShouldBe(70);
    }
}
