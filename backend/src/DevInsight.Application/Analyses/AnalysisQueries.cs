using DevInsight.Application.Abstractions;
using DevInsight.Application.Common;
using DevInsight.Application.Repositories;
using DevInsight.Domain.Analyses;

namespace DevInsight.Application.Analyses;

/// <summary>Read side of analyses: latest results, score history, run status and feedback — all owner-scoped.</summary>
public sealed class AnalysisQueries(
    RepositoryQueries repositoryQueries,
    IAnalysisStore analyses,
    IAnalysisRunStore runs)
{
    public async Task<Analysis> GetLatestAsync(
        Guid userId, Guid repositoryId, AnalysisScope scope, CancellationToken cancellationToken)
    {
        await repositoryQueries.GetAsync(userId, repositoryId, cancellationToken);
        return await analyses.GetLatestAsync(repositoryId, scope, cancellationToken)
            ?? throw new NotFoundException("Analysis", $"{repositoryId}/{scope}");
    }

    public async Task<IReadOnlyList<ScoreSnapshot>> GetHistoryAsync(
        Guid userId, Guid repositoryId, AnalysisScope scope, CancellationToken cancellationToken)
    {
        await repositoryQueries.GetAsync(userId, repositoryId, cancellationToken);
        return await analyses.GetScoreHistoryAsync([repositoryId], scope, cancellationToken);
    }

    public async Task<Analysis> GetAsync(Guid userId, Guid analysisId, CancellationToken cancellationToken)
    {
        var analysis = await analyses.GetAsync(analysisId, cancellationToken)
            ?? throw new NotFoundException("Analysis", analysisId);
        await repositoryQueries.GetAsync(userId, analysis.RepositoryId, cancellationToken);
        return analysis;
    }

    public async Task<AnalysisRun> GetRunAsync(Guid userId, Guid runId, CancellationToken cancellationToken) =>
        (await runs.GetAsync(runId, cancellationToken)).OwnedBy(userId, "Analysis run", runId);

    public Task<IReadOnlyList<AnalysisRun>> ListRecentRunsAsync(Guid userId, CancellationToken cancellationToken) =>
        runs.ListRecentForUserAsync(userId, limit: 50, cancellationToken);
}
