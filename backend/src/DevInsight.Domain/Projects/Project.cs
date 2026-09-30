using DevInsight.Domain.Common;

namespace DevInsight.Domain.Projects;

/// <summary>A curated portfolio entry: a named story that can group several repositories.</summary>
public sealed class Project : IUserOwned
{
    private const int MaxImages = 10;

    private Project() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public List<string> ImageUrls { get; private set; } = [];
    public List<Guid> LinkedRepositoryIds { get; private set; } = [];
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Project Create(Guid userId, ProjectDetails details, DateTimeOffset now)
    {
        var project = new Project { Id = Ids.New(), UserId = userId, CreatedAt = now };
        project.Update(details, now);
        return project;
    }

    public void Update(ProjectDetails details, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(details.Name))
        {
            throw new DomainException("Project name is required.");
        }

        if (details.ImageUrls.Count > MaxImages)
        {
            throw new DomainException($"A project can have at most {MaxImages} images.");
        }

        var invalidImage = details.ImageUrls.FirstOrDefault(url => !IsHttpsUrl(url));
        if (invalidImage is not null)
        {
            throw new DomainException($"Image URL must be an absolute https URL: {invalidImage}");
        }

        Name = details.Name.Trim();
        Description = string.IsNullOrWhiteSpace(details.Description) ? null : details.Description.Trim();
        ImageUrls = [.. details.ImageUrls];
        LinkedRepositoryIds = [.. details.LinkedRepositoryIds.Distinct()];
        SortOrder = details.SortOrder;
        UpdatedAt = now;
    }

    private static bool IsHttpsUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps;
}

public sealed record ProjectDetails(
    string Name,
    string? Description,
    IReadOnlyList<string> ImageUrls,
    IReadOnlyList<Guid> LinkedRepositoryIds,
    int SortOrder);
