namespace DevInsight.Domain.Analyses;

/// <summary>Commit activity for the week starting on <see cref="WeekStart"/> (a Monday, UTC).</summary>
public sealed record ActivityWeek(DateOnly WeekStart, int Commits, int Additions, int Deletions);

/// <summary>A file and its line count, used for the "largest files" view.</summary>
public sealed record FileSize(string Path, int Lines);
