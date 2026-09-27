namespace DevInsight.Infrastructure.GitHub;

public sealed class GitHubOptions
{
    public const string Section = "GitHub";

    /// <summary>OAuth App client id (github.com → Settings → Developer settings → OAuth Apps).</summary>
    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Must match the OAuth App's callback URL, e.g. http://localhost:4200/api/auth/github/callback.</summary>
    public string CallbackUrl { get; set; } = string.Empty;

    /// <summary>
    /// Requests the <c>repo</c> scope so private repositories can be imported and analysed. Off by
    /// default: public repositories need no repository scope at all, and <c>repo</c> grants write access.
    /// </summary>
    public bool IncludePrivateRepositories { get; set; }

    public string ProductName { get; set; } = "DevInsight";
}
