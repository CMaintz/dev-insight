using DevInsight.Domain.Analyses;
using DevInsight.Domain.Analyses.Engine;
using static DevInsight.Domain.Tests.TestData;

namespace DevInsight.Domain.Tests;

public class AnalysisEngineTests
{
    private static RepositorySnapshot Snapshot(IReadOnlyList<CommitRecord> commits, IReadOnlyList<SourceFile>? files = null) =>
        new("abc123", commits, files ?? HealthyFiles());

    [Fact]
    public void Every_dimension_score_is_the_weighted_sum_of_its_metrics()
    {
        var result = AnalysisEngine.Analyze(Snapshot(WeeklyCommits(10)), AnalysisScope.Repo, Identity, Now);

        foreach (var (category, score) in new[]
                 {
                     (MetricCategory.Activity, result.ActivityScore),
                     (MetricCategory.Structure, result.StructureScore),
                     (MetricCategory.Quality, result.QualityScore),
                 })
        {
            var scored = result.Metrics.Where(m => m.Category == category && m.IncludedInScore).ToList();
            scored.Sum(m => m.Weight!.Value).ShouldBe(1, 1e-9, $"{category} weights");
            score.ShouldBe(Scoring.Dimension(scored), $"{category} score");
        }
    }

    [Fact]
    public void Overall_score_weights_dimensions_30_30_40() =>
        Scoring.Overall(activity: 50, structure: 100, quality: 0).ShouldBe(45);

    [Fact]
    public void Metric_names_are_unique()
    {
        var result = AnalysisEngine.Analyze(Snapshot(WeeklyCommits(3)), AnalysisScope.Repo, Identity, Now);
        result.Metrics.Select(m => m.Name).ShouldBeUnique();
    }

    [Fact]
    public void User_scope_counts_only_the_users_commits()
    {
        var commits = WeeklyCommits(4).Concat(WeeklyCommits(6, OtherEmail)).ToList();
        var repo = AnalysisEngine.Analyze(Snapshot(commits), AnalysisScope.Repo, Identity, Now);
        var user = AnalysisEngine.Analyze(Snapshot(commits), AnalysisScope.UserContribution, Identity, Now);

        repo.Metrics.Single(m => m.Name == MetricKeys.TotalCommits).Value.ShouldBe(10);
        user.Metrics.Single(m => m.Name == MetricKeys.TotalCommits).Value.ShouldBe(4);
        user.Metrics.Single(m => m.Name == MetricKeys.ContributorCount).Value.ShouldBe(2);
    }

    [Fact]
    public void User_scope_measures_structure_over_touched_files_only()
    {
        var files = new[] { File("src/mine.ts", 100), File("src/theirs.ts", 3000) };
        var commits = new[] { Commit(1, paths: "src/mine.ts"), Commit(2, email: OtherEmail, paths: "src/theirs.ts") };
        var user = AnalysisEngine.Analyze(Snapshot(commits, files), AnalysisScope.UserContribution, Identity, Now);

        user.Metrics.Single(m => m.Name == MetricKeys.FilesOver500Lines).Value.ShouldBe(0);
        user.LargestFiles.ShouldHaveSingleItem().Path.ShouldBe("src/mine.ts");
    }

    [Fact]
    public void Timeline_has_a_bucket_for_every_week()
    {
        var commits = new[] { Commit(daysAgo: 1), Commit(daysAgo: 22), Commit(daysAgo: 23) };
        var result = AnalysisEngine.Analyze(Snapshot(commits), AnalysisScope.Repo, Identity, Now);

        result.Timeline.Count.ShouldBeInRange(4, 5);
        result.Timeline.Sum(w => w.Commits).ShouldBe(3);
        result.Timeline.ShouldAllBe(w => w.WeekStart.DayOfWeek == DayOfWeek.Monday);
        result.Timeline.Select(w => w.WeekStart).ShouldBeInOrder();
    }

    [Fact]
    public void Timeline_is_capped_at_two_years()
    {
        var result = AnalysisEngine.Analyze(Snapshot([Commit(daysAgo: 2000), Commit(daysAgo: 1)]), AnalysisScope.Repo, Identity, Now);
        result.Timeline.Count.ShouldBe(104);
    }

    [Fact]
    public void Largest_files_are_the_top_ten_source_files()
    {
        var files = Enumerable.Range(1, 15).Select(i => File($"src/f{i}.cs", i * 10)).Append(File("data.csv", 99999)).ToList();
        var result = AnalysisEngine.Analyze(Snapshot([Commit(1)], files), AnalysisScope.Repo, Identity, Now);

        result.LargestFiles.Count.ShouldBe(10);
        result.LargestFiles[0].ShouldBe(new FileSize("src/f15.cs", 150));
    }
}
