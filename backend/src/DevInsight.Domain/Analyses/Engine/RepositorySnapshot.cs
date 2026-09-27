namespace DevInsight.Domain.Analyses.Engine;

/// <summary>
/// Everything the analysis engine needs to know about a repository at one moment: its commit
/// history and the files at HEAD. Produced by an infrastructure adapter; the engine never talks
/// to git or GitHub itself.
/// </summary>
public sealed record RepositorySnapshot(
    string? HeadCommitSha,
    IReadOnlyList<CommitRecord> Commits,
    IReadOnlyList<SourceFile> Files);

/// <param name="Subject">The first line of the commit message.</param>
/// <param name="Paths">Paths changed by the commit.</param>
public sealed record CommitRecord(
    string Sha,
    string AuthorName,
    string AuthorEmail,
    DateTimeOffset AuthoredAt,
    string Subject,
    int Additions,
    int Deletions,
    bool IsMerge,
    IReadOnlyList<string> Paths)
{
    public int LinesChanged => Additions + Deletions;
}

/// <summary>A text file at HEAD, with its repository-relative path (forward slashes) and line count.</summary>
public sealed record SourceFile(string Path, int Lines);
