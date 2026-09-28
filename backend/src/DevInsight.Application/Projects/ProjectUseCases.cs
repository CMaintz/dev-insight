using DevInsight.Application.Abstractions;
using DevInsight.Application.Common;
using DevInsight.Domain.Common;
using DevInsight.Domain.Projects;

namespace DevInsight.Application.Projects;

/// <summary>Portfolio projects: curated entries that group repositories and carry images and a description.</summary>
public sealed class ProjectUseCases(
    IProjectStore projects,
    IRepositoryStore repositories,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<Project>> ListAsync(Guid userId, CancellationToken cancellationToken) =>
        [.. (await projects.ListForUserAsync(userId, cancellationToken)).OrderBy(p => p.SortOrder).ThenBy(p => p.CreatedAt)];

    public async Task<Project> CreateAsync(Guid userId, ProjectDetails details, CancellationToken cancellationToken)
    {
        await EnsureOwnRepositoriesAsync(userId, details.LinkedRepositoryIds, cancellationToken);
        var project = Project.Create(userId, details, clock.GetUtcNow());
        projects.Add(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return project;
    }

    public async Task<Project> UpdateAsync(
        Guid userId, Guid projectId, ProjectDetails details, CancellationToken cancellationToken)
    {
        var project = await GetOwnedAsync(userId, projectId, cancellationToken);
        await EnsureOwnRepositoriesAsync(userId, details.LinkedRepositoryIds, cancellationToken);
        project.Update(details, clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return project;
    }

    public async Task DeleteAsync(Guid userId, Guid projectId, CancellationToken cancellationToken)
    {
        projects.Remove(await GetOwnedAsync(userId, projectId, cancellationToken));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Project> GetOwnedAsync(Guid userId, Guid projectId, CancellationToken cancellationToken) =>
        (await projects.GetAsync(projectId, cancellationToken)).OwnedBy(userId, "Project", projectId);

    private async Task EnsureOwnRepositoriesAsync(
        Guid userId, IReadOnlyList<Guid> repositoryIds, CancellationToken cancellationToken)
    {
        var owned = (await repositories.ListForUserAsync(userId, cancellationToken)).Select(r => r.Id).ToHashSet();
        var foreign = repositoryIds.Where(id => !owned.Contains(id)).ToList();
        if (foreign.Count > 0)
        {
            throw new DomainException($"Unknown repositories: {string.Join(", ", foreign)}");
        }
    }
}
