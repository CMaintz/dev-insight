using DevInsight.Domain.Common;

namespace DevInsight.Domain.Repositories;

/// <summary>A GitHub repository imported by a user.</summary>
public sealed class Repository : IUserOwned
{
    private Repository() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public long GitHubRepoId { get; private set; }
    public string Owner { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string FullName => $"{Owner}/{Name}";
    public string? Description { get; private set; }
    public string HtmlUrl { get; private set; } = string.Empty;
    public string? PrimaryLanguage { get; private set; }
    public int Stars { get; private set; }
    public int Forks { get; private set; }
    public bool IsFork { get; private set; }
    public bool IsPrivate { get; private set; }
    public bool IsArchived { get; private set; }
    public string DefaultBranch { get; private set; } = "main";
    public long SizeKb { get; private set; }
    public DateTimeOffset? LastActivity { get; private set; }

    /// <summary>Controls inclusion in the public portfolio and in aggregate metrics.</summary>
    public bool IsSelected { get; private set; }

    /// <summary>Bytes of code per language, as measured by GitHub Linguist.</summary>
    public Dictionary<string, long> Languages { get; private set; } = [];

    public DateTimeOffset ImportedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// New imports are selected by default when the user owns them. Forks and repositories owned by
    /// others (organisations, collaborations) are mostly someone else's work, so they are opt-in.
    /// </summary>
    public static Repository Import(Guid userId, string userLogin, GitHubRepositoryInfo info, DateTimeOffset now)
    {
        var repository = new Repository
        {
            Id = Ids.New(),
            UserId = userId,
            GitHubRepoId = info.GitHubRepoId,
            IsSelected = !info.IsFork && string.Equals(info.Owner, userLogin, StringComparison.OrdinalIgnoreCase),
            ImportedAt = now,
        };
        repository.Refresh(info, now);
        return repository;
    }

    public void Refresh(GitHubRepositoryInfo info, DateTimeOffset now)
    {
        if (info.GitHubRepoId != GitHubRepoId)
        {
            throw new DomainException("Repository metadata belongs to a different GitHub repository.");
        }

        Owner = info.Owner;
        Name = info.Name;
        Description = info.Description;
        HtmlUrl = info.HtmlUrl;
        PrimaryLanguage = info.PrimaryLanguage;
        Stars = info.Stars;
        Forks = info.Forks;
        IsFork = info.IsFork;
        IsPrivate = info.IsPrivate;
        IsArchived = info.IsArchived;
        DefaultBranch = info.DefaultBranch;
        SizeKb = info.SizeKb;
        LastActivity = info.LastActivity;
        UpdatedAt = now;
    }

    public void SetLanguages(IReadOnlyDictionary<string, long> languages) =>
        Languages = new Dictionary<string, long>(languages);

    public void SetSelected(bool isSelected, DateTimeOffset now)
    {
        IsSelected = isSelected;
        UpdatedAt = now;
    }
}
