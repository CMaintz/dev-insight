using DevInsight.Domain.Analyses;
using DevInsight.Domain.Analyses.Engine;
using DevInsight.Domain.Repositories;
using DevInsight.Domain.Users;

namespace DevInsight.Application.Abstractions;

/// <summary>Outbound port to the GitHub API.</summary>
public interface IGitHubGateway
{
    /// <summary>The URL that starts the OAuth authorization-code flow.</summary>
    Uri GetAuthorizationUrl(string state);

    Task<string> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken);

    Task<GitHubIdentity> GetIdentityAsync(string accessToken, CancellationToken cancellationToken);

    Task<IReadOnlyList<GitHubRepositoryInfo>> ListRepositoriesAsync(string accessToken, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, long>> GetLanguagesAsync(
        string accessToken, string owner, string name, CancellationToken cancellationToken);
}

/// <summary>Outbound port that captures commit history and HEAD files of a repository.</summary>
public interface IRepositorySnapshotSource
{
    Task<RepositorySnapshot> CaptureAsync(Repository repository, string accessToken, CancellationToken cancellationToken);
}

/// <summary>Stores each user's GitHub access token, encrypted at rest.</summary>
public interface IGitHubCredentialStore
{
    Task SaveAsync(Guid userId, string accessToken, CancellationToken cancellationToken);

    Task<string?> GetAsync(Guid userId, CancellationToken cancellationToken);
}

/// <summary>Optional AI feedback, grounded in the analysis metrics. Disabled when no provider is configured.</summary>
public interface IAiFeedbackGenerator
{
    bool IsEnabled { get; }

    Task<IReadOnlyList<FeedbackFinding>> GenerateAsync(AiFeedbackRequest request, CancellationToken cancellationToken);
}

public sealed record AiFeedbackRequest(
    string RepositoryName,
    string? PrimaryLanguage,
    Analysis Analysis,
    IReadOnlyList<Feedback> RuleFeedback);

/// <summary>Hands analysis runs from the API to the background worker.</summary>
public interface IAnalysisQueue
{
    ValueTask EnqueueAsync(Guid runId, CancellationToken cancellationToken);

    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken);
}
