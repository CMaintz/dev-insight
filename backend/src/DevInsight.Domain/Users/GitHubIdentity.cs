namespace DevInsight.Domain.Users;

/// <summary>The GitHub account a user authenticated as, as reported by the GitHub API.</summary>
public sealed record GitHubIdentity(
    long GitHubId,
    string Login,
    string? Name,
    string? Email,
    string? AvatarUrl,
    IReadOnlyList<string> VerifiedEmails);
