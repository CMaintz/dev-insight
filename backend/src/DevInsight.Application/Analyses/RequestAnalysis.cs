using DevInsight.Application.Abstractions;
using DevInsight.Application.Repositories;
using DevInsight.Domain.Analyses;

namespace DevInsight.Application.Analyses;

/// <summary>
/// UC3 (entry): queue an analysis run. Idempotent per repository and scope — while a run is
/// queued or running, requesting again returns that run instead of queueing a duplicate.
/// </summary>
public sealed class RequestAnalysis(
    RepositoryQueries repositoryQueries,
    IAnalysisRunStore runs,
    IAnalysisQueue queue,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<AnalysisRun> ExecuteAsync(
        Guid userId, Guid repositoryId, AnalysisScope scope, CancellationToken cancellationToken)
    {
        var repository = await repositoryQueries.GetAsync(userId, repositoryId, cancellationToken);
        var pending = await runs.FindUnfinishedAsync(repository.Id, scope, cancellationToken);
        if (pending is not null)
        {
            return pending;
        }

        var run = AnalysisRun.Queue(userId, repository.Id, scope, clock.GetUtcNow());
        runs.Add(run);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await queue.EnqueueAsync(run.Id, cancellationToken);
        return run;
    }

    /// <summary>Queues both scopes for every selected repository.</summary>
    public async Task<IReadOnlyList<AnalysisRun>> ExecuteForSelectedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var selected = (await repositoryQueries.ListAsync(userId, cancellationToken)).Where(r => r.IsSelected);
        var queued = new List<AnalysisRun>();
        foreach (var repository in selected)
        {
            foreach (var scope in Enum.GetValues<AnalysisScope>())
            {
                queued.Add(await ExecuteAsync(userId, repository.Id, scope, cancellationToken));
            }
        }

        return queued;
    }
}
