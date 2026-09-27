namespace DevInsight.Domain.Repositories;

/// <summary>Repository metadata as reported by the GitHub API.</summary>
public sealed record GitHubRepositoryInfo(
    long GitHubRepoId,
    string Owner,
    string Name,
    string? Description,
    string HtmlUrl,
    string? PrimaryLanguage,
    int Stars,
    int Forks,
    bool IsFork,
    bool IsPrivate,
    bool IsArchived,
    string DefaultBranch,
    long SizeKb,
    DateTimeOffset? LastActivity);
