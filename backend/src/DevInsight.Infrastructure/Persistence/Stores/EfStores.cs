using DevInsight.Application.Abstractions;
using DevInsight.Domain.Analyses;
using DevInsight.Domain.Projects;
using DevInsight.Domain.Repositories;
using DevInsight.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DevInsight.Infrastructure.Persistence.Stores;

internal sealed class EfUserStore(DevInsightDbContext db) : IUserStore
{
    public Task<User?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> FindByGitHubIdAsync(long gitHubId, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(u => u.GitHubId == gitHubId, cancellationToken);

    public Task<User?> FindByLoginAsync(string login, CancellationToken cancellationToken) =>
        db.Users
            .Where(u => u.Login.ToLower() == login.ToLower())
            .OrderByDescending(u => u.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListIdsAsync(CancellationToken cancellationToken) =>
        await db.Users.Select(u => u.Id).ToListAsync(cancellationToken);

    public void Add(User user) => db.Users.Add(user);
}

internal sealed class EfRepositoryStore(DevInsightDbContext db) : IRepositoryStore
{
    public Task<Repository?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Repositories.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Repository>> ListForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.Repositories.Where(r => r.UserId == userId).ToListAsync(cancellationToken);

    public void Add(Repository repository) => db.Repositories.Add(repository);
}

internal sealed class EfAnalysisRunStore(DevInsightDbContext db) : IAnalysisRunStore
{
    public Task<AnalysisRun?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.AnalysisRuns.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    private IQueryable<AnalysisRun> Unfinished =>
        db.AnalysisRuns.Where(r => r.Status == AnalysisRunStatus.Queued || r.Status == AnalysisRunStatus.Running);

    public Task<AnalysisRun?> FindUnfinishedAsync(Guid repositoryId, AnalysisScope scope, CancellationToken cancellationToken) =>
        Unfinished
            .Where(r => r.RepositoryId == repositoryId && r.Scope == scope)
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AnalysisRun>> ListUnfinishedAsync(CancellationToken cancellationToken) =>
        await Unfinished
            .OrderBy(r => r.RequestedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AnalysisRun>> ListRecentForUserAsync(Guid userId, int limit, CancellationToken cancellationToken) =>
        await db.AnalysisRuns
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RequestedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public void Add(AnalysisRun run) => db.AnalysisRuns.Add(run);
}

internal sealed class EfProjectStore(DevInsightDbContext db) : IProjectStore
{
    public Task<Project?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Project>> ListForUserAsync(Guid userId, CancellationToken cancellationToken) =>
        await db.Projects.Where(p => p.UserId == userId).ToListAsync(cancellationToken);

    public void Add(Project project) => db.Projects.Add(project);

    public void Remove(Project project) => db.Projects.Remove(project);
}
