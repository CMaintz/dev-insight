using DevInsight.Application.Abstractions;
using DevInsight.Domain.Analyses;
using Microsoft.EntityFrameworkCore;

namespace DevInsight.Infrastructure.Persistence.Stores;

internal sealed class EfAnalysisStore(DevInsightDbContext db) : IAnalysisStore
{
    private IQueryable<Analysis> WithDetails =>
        db.Analyses.Include(a => a.Metrics).Include(a => a.Feedback).AsSplitQuery();

    public Task<Analysis?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        WithDetails.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<Analysis?> GetLatestAsync(Guid repositoryId, AnalysisScope scope, CancellationToken cancellationToken) =>
        WithDetails
            .Where(a => a.RepositoryId == repositoryId && a.Scope == scope)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Analysis>> GetLatestForRepositoriesAsync(
        IReadOnlyCollection<Guid> repositoryIds, AnalysisScope scope, CancellationToken cancellationToken)
    {
        if (repositoryIds.Count == 0)
        {
            return [];
        }

        var latestIds = await db.Analyses
            .Where(a => repositoryIds.Contains(a.RepositoryId) && a.Scope == scope)
            .GroupBy(a => a.RepositoryId)
            .Select(g => g.OrderByDescending(a => a.CreatedAt).Select(a => a.Id).First())
            .ToListAsync(cancellationToken);

        return await WithDetails.Where(a => latestIds.Contains(a.Id)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScoreSnapshot>> GetScoreHistoryAsync(
        IReadOnlyCollection<Guid> repositoryIds, AnalysisScope scope, CancellationToken cancellationToken)
    {
        if (repositoryIds.Count == 0)
        {
            return [];
        }

        return await db.Analyses
            .Where(a => repositoryIds.Contains(a.RepositoryId) && a.Scope == scope)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new ScoreSnapshot(
                a.Id, a.RepositoryId, a.CreatedAt, a.OverallScore, a.ActivityScore, a.StructureScore, a.QualityScore))
            .ToListAsync(cancellationToken);
    }

    public void Add(Analysis analysis) => db.Analyses.Add(analysis);
}
