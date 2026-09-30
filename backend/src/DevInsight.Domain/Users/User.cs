using DevInsight.Domain.Common;

namespace DevInsight.Domain.Users;

public sealed class User
{
    private const int MaxBioLength = 2000;

    private User() { }

    public Guid Id { get; private set; }
    public long GitHubId { get; private set; }
    public string Login { get; private set; } = string.Empty;
    public string? Name { get; private set; }
    public string? Email { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? Bio { get; private set; }
    public string? LinkedInUrl { get; private set; }
    public bool IsPortfolioPublic { get; private set; }

    /// <summary>Every email that attributes a commit to this user (verified + GitHub noreply addresses).</summary>
    public List<string> CommitEmails { get; private set; } = [];

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static User Register(GitHubIdentity identity, DateTimeOffset now)
    {
        var user = new User { Id = Ids.New(), GitHubId = identity.GitHubId, CreatedAt = now };
        user.SyncWith(identity, now);
        return user;
    }

    public void SyncWith(GitHubIdentity identity, DateTimeOffset now)
    {
        if (identity.GitHubId != GitHubId)
        {
            throw new DomainException("A user cannot be re-linked to a different GitHub account.");
        }

        Login = identity.Login;
        Name = identity.Name;
        Email = identity.Email;
        AvatarUrl = identity.AvatarUrl;
        CommitEmails = BuildCommitEmails(identity);
        UpdatedAt = now;
    }

    public void UpdateProfile(string? bio, string? linkedInUrl, bool isPortfolioPublic, DateTimeOffset now)
    {
        var trimmedBio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
        if (trimmedBio?.Length > MaxBioLength)
        {
            throw new DomainException($"Bio must be at most {MaxBioLength} characters.");
        }

        Bio = trimmedBio;
        LinkedInUrl = NormalizeLinkedInUrl(linkedInUrl);
        IsPortfolioPublic = isPortfolioPublic;
        UpdatedAt = now;
    }

    public ContributorIdentity ToContributorIdentity() => new(Login, CommitEmails);

    private static List<string> BuildCommitEmails(GitHubIdentity identity)
    {
        var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            $"{identity.GitHubId}+{identity.Login}@users.noreply.github.com",
            $"{identity.Login}@users.noreply.github.com",
        };
        if (!string.IsNullOrWhiteSpace(identity.Email))
        {
            emails.Add(identity.Email);
        }

        emails.UnionWith(identity.VerifiedEmails);
        return [.. emails.Select(e => e.ToLowerInvariant()).Order(StringComparer.Ordinal)];
    }

    private static string? NormalizeLinkedInUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var isLinkedIn = Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && (uri.Host == "linkedin.com" || uri.Host.EndsWith(".linkedin.com", StringComparison.OrdinalIgnoreCase));
        if (!isLinkedIn)
        {
            throw new DomainException("LinkedIn URL must be an https://linkedin.com address.");
        }

        return uri!.ToString();
    }
}
