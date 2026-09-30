using DevInsight.Domain.Analyses;
using DevInsight.Domain.Projects;
using DevInsight.Domain.Repositories;
using DevInsight.Domain.Users;

namespace DevInsight.Application.Abstractions;

/// <summary>Persists all tracked changes made through the stores as one transaction.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IUserStore
{
    Task<User?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> FindByGitHubIdAsync(long gitHubId, CancellationToken cancellationToken);

    Task<User?> FindByLoginAsync(string login, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ListIdsAsync(CancellationToken cancellationToken);

    void Add(User user);
}

public interface IRepositoryStore
{
    Task<Repository?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Repository>> ListForUserAsync(Guid userId, CancellationToken cancellationToken);

    void Add(Repository repository);
}

public interface IAnalysisStore
{
    /// <summary>Loads an analysis with its metrics and feedback.</summary>
    Task<Analysis?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<Analysis?> GetLatestAsync(Guid repositoryId, AnalysisScope scope, CancellationToken cancellationToken);

    /// <summary>The latest analysis (with metrics and feedback) of each given repository in the scope.</summary>
    Task<IReadOnlyList<Analysis>> GetLatestForRepositoriesAsync(
        IReadOnlyCollection<Guid> repositoryIds, AnalysisScope scope, CancellationToken cancellationToken);

    /// <summary>Score snapshots (no metrics) for the repositories, oldest first.</summary>
    Task<IReadOnlyList<ScoreSnapshot>> GetScoreHistoryAsync(
        IReadOnlyCollection<Guid> repositoryIds, AnalysisScope scope, CancellationToken cancellationToken);

    void Add(Analysis analysis);
}

public sealed record ScoreSnapshot(
    Guid AnalysisId,
    Guid RepositoryId,
    DateTimeOffset CreatedAt,
    int OverallScore,
    int ActivityScore,
    int StructureScore,
    int QualityScore);

public interface IAnalysisRunStore
{
    Task<AnalysisRun?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<AnalysisRun?> FindUnfinishedAsync(Guid repositoryId, AnalysisScope scope, CancellationToken cancellationToken);

    Task<IReadOnlyList<AnalysisRun>> ListUnfinishedAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<AnalysisRun>> ListRecentForUserAsync(Guid userId, int limit, CancellationToken cancellationToken);

    void Add(AnalysisRun run);
}

public interface IProjectStore
{
    Task<Project?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Project>> ListForUserAsync(Guid userId, CancellationToken cancellationToken);

    void Add(Project project);

    void Remove(Project project);
}
