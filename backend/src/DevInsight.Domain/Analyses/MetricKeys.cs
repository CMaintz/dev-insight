namespace DevInsight.Domain.Analyses;

/// <summary>Stable metric identifiers — the vocabulary shared by the engine, the feedback rules and the UI.</summary>
public static class MetricKeys
{
    // Activity
    public const string TotalCommits = "total_commits";
    public const string DaysSinceLastCommit = "days_since_last_commit";
    public const string CommitsPerWeekRecent = "commits_per_week_12w";
    public const string CommitsPerWeekLifetime = "commits_per_week_lifetime";
    public const string ActiveWeeksRatio = "active_weeks_ratio_26w";
    public const string IsDormant = "is_dormant";

    // Commit quality
    public const string CommitMessageQuality = "commit_message_quality";
    public const string VagueCommitRatio = "vague_commit_ratio";
    public const string VagueCommitCount = "vague_commit_count";
    public const string AverageCommitSize = "average_commit_size";
    public const string CommitSizeStdDev = "commit_size_stddev";
    public const string LargeCommitRatio = "large_commit_ratio";
    public const string CommitsSizeXs = "commits_size_xs";
    public const string CommitsSizeS = "commits_size_s";
    public const string CommitsSizeM = "commits_size_m";
    public const string CommitsSizeL = "commits_size_l";
    public const string CommitsSizeXl = "commits_size_xl";

    // Structure
    public const string SourceFileCount = "source_file_count";
    public const string TotalLinesOfCode = "total_loc";
    public const string MedianFileLines = "median_file_loc";
    public const string LargestFileLines = "largest_file_loc";
    public const string FilesOver500Lines = "files_over_500_loc";
    public const string LargeFileRatio = "large_file_ratio";
    public const string MaxFolderDepth = "max_folder_depth";
    public const string AverageFolderDepth = "average_folder_depth";
    public const string MonolithIndicator = "monolith_indicator";

    // Quality heuristics
    public const string HasTests = "has_tests";
    public const string TestFileRatio = "test_file_ratio";
    public const string HasReadme = "has_readme";
    public const string HasLintConfig = "has_lint_config";
    public const string HasCi = "has_ci";
    public const string HasLicense = "has_license";
    public const string ContributorCount = "contributor_count";
}
