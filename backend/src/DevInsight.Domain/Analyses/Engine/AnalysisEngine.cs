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
        var (commits, files) = InScope(snapshot, scope, contributor);
        var activity = ActivityAnalyzer.Analyze(commits, now);
        var structure = StructureAnalyzer.Analyze(files);
        var quality = QualityAnalyzer.Analyze(
            files, snapshot.Files, CommitQualityAnalyzer.MessageQuality(commits), ContributorCount(snapshot.Commits));

        return new AnalysisResult(
            snapshot.HeadCommitSha,
            ActivityScore: Scoring.Dimension(activity),
            StructureScore: Scoring.Dimension(structure),
            QualityScore: Scoring.Dimension(quality),
            Metrics: [.. activity, .. CommitQualityAnalyzer.Analyze(commits), .. structure, .. quality],
            Timeline: BuildTimeline(commits, now),
            LargestFiles: LargestFiles(files));
    }

    /// <summary>The commits and files an analysis in <paramref name="scope"/> looks at.</summary>
    private static (IReadOnlyList<CommitRecord> Commits, IReadOnlyList<SourceFile> Files) InScope(
        RepositorySnapshot snapshot, AnalysisScope scope, ContributorIdentity contributor)
    {
        if (scope == AnalysisScope.Repo)
        {
            return (snapshot.Commits, snapshot.Files);
        }

        List<CommitRecord> own = [.. snapshot.Commits.Where(c => contributor.Authored(c.AuthorEmail))];
        var touched = own.SelectMany(c => c.Paths).ToHashSet(StringComparer.Ordinal);
        return (own, [.. snapshot.Files.Where(f => touched.Contains(f.Path))]);
    }

    private static int ContributorCount(IReadOnlyList<CommitRecord> commits) =>
        commits.Select(c => c.AuthorEmail.ToLowerInvariant()).Distinct(StringComparer.Ordinal).Count();

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
