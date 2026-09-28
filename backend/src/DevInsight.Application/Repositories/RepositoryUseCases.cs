using DevInsight.Application.Abstractions;
using DevInsight.Application.Common;
using DevInsight.Domain.Repositories;

namespace DevInsight.Application.Repositories;

public sealed record ImportSummary(int Imported, int Updated, int Total);

/// <summary>UC2: fetch the user's repositories from GitHub and upsert them — never duplicating.</summary>
public sealed class ImportRepositories(
    IGitHubGateway gitHub,
    IUserStore users,
    IGitHubCredentialStore credentials,
    IRepositoryStore repositories,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    private const int LanguageFetchParallelism = 4;

    public async Task<ImportSummary> ExecuteAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetAsync(userId, cancellationToken) ?? throw new NotFoundException("User", userId);
        var token = await credentials.GetAsync(userId, cancellationToken)
            ?? throw new PreconditionFailedException("No GitHub token on file. Sign in with GitHub again.");
        var remote = await gitHub.ListRepositoriesAsync(token, cancellationToken);
        var existing = (await repositories.ListForUserAsync(userId, cancellationToken)).ToDictionary(r => r.GitHubRepoId);
        var updated = remote.Count(info => existing.ContainsKey(info.GitHubRepoId));
        var now = clock.GetUtcNow();
        List<Repository> synced = [.. remote.Select(info => Upsert(user.Login, userId, info, existing, now))];

        await FetchLanguagesAsync(token, synced, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ImportSummary(remote.Count - updated, updated, remote.Count);
    }

    /// <summary>Refreshes the repository if it was imported before; otherwise imports it.</summary>
    private Repository Upsert(
        string login, Guid userId, GitHubRepositoryInfo info, Dictionary<long, Repository> existing, DateTimeOffset now)
    {
        if (existing.TryGetValue(info.GitHubRepoId, out var repository))
        {
            repository.Refresh(info, now);
            return repository;
        }

        repository = Repository.Import(userId, login, info, now);
        repositories.Add(repository);
        return repository;
    }

    private async Task FetchLanguagesAsync(string token, List<Repository> synced, CancellationToken cancellationToken)
    {
        var languages = new Dictionary<Guid, IReadOnlyDictionary<string, long>>();
        await Parallel.ForEachAsync(
            synced,
            new ParallelOptions { MaxDegreeOfParallelism = LanguageFetchParallelism, CancellationToken = cancellationToken },
            async (repository, ct) =>
            {
                var result = await gitHub.GetLanguagesAsync(token, repository.Owner, repository.Name, ct);
                lock (languages)
                {
                    languages[repository.Id] = result;
                }
            });

        foreach (var repository in synced)
        {
            repository.SetLanguages(languages[repository.Id]);
        }
    }
}

/// <summary>Repository listing and UC2.1 selection.</summary>
public sealed class RepositoryQueries(IRepositoryStore repositories, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public async Task<IReadOnlyList<Repository>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        [.. (await repositories.ListForUserAsync(userId, cancellationToken))
            .OrderByDescending(r => r.LastActivity)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)];

    public async Task<Repository> GetAsync(Guid userId, Guid repositoryId, CancellationToken cancellationToken) =>
        (await repositories.GetAsync(repositoryId, cancellationToken)).OwnedBy(userId, "Repository", repositoryId);

    public async Task<Repository> SetSelectedAsync(
        Guid userId, Guid repositoryId, bool isSelected, CancellationToken cancellationToken)
    {
        var repository = await GetAsync(userId, repositoryId, cancellationToken);
        repository.SetSelected(isSelected, clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return repository;
    }
}
