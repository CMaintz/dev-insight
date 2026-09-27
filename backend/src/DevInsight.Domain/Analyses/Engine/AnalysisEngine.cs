using DevInsight.Domain.Users;

namespace DevInsight.Domain.Analyses.Engine;

/// <summary>
/// Pure function from a repository snapshot to scored, explainable metrics.
/// In <see cref="AnalysisScope.UserContribution"/> scope, activity and commit metrics use only the
/// user's commits, and structure/test metrics use only the files the user touched. Project hygiene
/// (README, lint, CI) stays repository-wide: it describes the project the work lives in.
/// </summary>
public static class AnalysisEngine
{
    private const int TimelineWeeks = 104;
    private const int LargestFileCount = 10;

    public static AnalysisResult Analyze(
        RepositorySnapshot snapshot,
        AnalysisScope scope,
        ContributorIdentity contributor,
        DateTimeOffset now)
    {
        var commits = scope == AnalysisScope.Repo
            ? snapshot.Commits
            : [.. snapshot.Commits.Where(c => contributor.Authored(c.AuthorEmail))];
        var files = scope == AnalysisScope.Repo ? snapshot.Files : TouchedFiles(snapshot.Files, commits);
        var contributors = snapshot.Commits
            .Select(c => c.AuthorEmail.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .Count();

        var activity = ActivityAnalyzer.Analyze(commits, now);
        var commitQuality = CommitQualityAnalyzer.Analyze(commits);
        var structure = StructureAnalyzer.Analyze(files);
        var quality = QualityAnalyzer.Analyze(
            files, snapshot.Files, CommitQualityAnalyzer.MessageQuality(commits), contributors);

        return new AnalysisResult(
            snapshot.HeadCommitSha,
            ActivityScore: Scoring.Dimension(activity),
            StructureScore: Scoring.Dimension(structure),
            QualityScore: Scoring.Dimension(quality),
            Metrics: [.. activity, .. commitQuality, .. structure, .. quality],
            Timeline: BuildTimeline(commits, now),
            LargestFiles: LargestFiles(files));
    }

    private static List<SourceFile> TouchedFiles(IReadOnlyList<SourceFile> files, IReadOnlyList<CommitRecord> commits)
    {
        var touched = commits.SelectMany(c => c.Paths).ToHashSet(StringComparer.Ordinal);
        return [.. files.Where(f => touched.Contains(f.Path))];
    }

    /// <summary>Weekly buckets from the first commit (at most two years back) up to this week, gaps included.</summary>
    private static List<ActivityWeek> BuildTimeline(IReadOnlyList<CommitRecord> commits, DateTimeOffset now)
    {
        if (commits.Count == 0)
        {
            return [];
        }

        var byWeek = commits
            .GroupBy(c => Weeks.StartOf(c.AuthoredAt))
            .ToDictionary(g => g.Key, g => g.ToList());
        var thisWeek = Weeks.StartOf(now);
        var earliestAllowed = thisWeek.AddDays(-7 * (TimelineWeeks - 1));
        var firstWeek = byWeek.Keys.Min();
        var start = firstWeek > earliestAllowed ? firstWeek : earliestAllowed;

        var timeline = new List<ActivityWeek>();
        for (var week = start; week <= thisWeek; week = week.AddDays(7))
        {
            var inWeek = byWeek.GetValueOrDefault(week) ?? [];
            timeline.Add(new ActivityWeek(week, inWeek.Count, inWeek.Sum(c => c.Additions), inWeek.Sum(c => c.Deletions)));
        }

        return timeline;
    }

    private static List<FileSize> LargestFiles(IReadOnlyList<SourceFile> files) =>
    [
        .. files
            .Where(f => FileClassifier.IsSource(f.Path))
            .OrderByDescending(f => f.Lines)
            .ThenBy(f => f.Path, StringComparer.Ordinal)
            .Take(LargestFileCount)
            .Select(f => new FileSize(f.Path, f.Lines)),
    ];
}

public sealed record AnalysisResult(
    string? HeadCommitSha,
    int ActivityScore,
    int StructureScore,
    int QualityScore,
    IReadOnlyList<AnalysisMetric> Metrics,
    IReadOnlyList<ActivityWeek> Timeline,
    IReadOnlyList<FileSize> LargestFiles);
