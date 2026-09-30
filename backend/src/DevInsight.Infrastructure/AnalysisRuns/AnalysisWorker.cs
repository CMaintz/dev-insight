using System.Threading.Channels;
using DevInsight.Application.Abstractions;
using DevInsight.Application.Analyses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevInsight.Infrastructure.AnalysisRuns;

public sealed class AnalysisWorkerOptions
{
    public const string Section = "AnalysisWorker";

    public bool Enabled { get; set; } = true;

    public int MaxConcurrency { get; set; } = 2;

    /// <summary>Re-analyse every user's selected repositories on this interval (score snapshots). 0 disables.</summary>
    public double ScheduledReanalysisHours { get; set; } = 24;
}

/// <summary>In-process queue. Runs are persisted first, so nothing is lost if the process restarts.</summary>
internal sealed class ChannelAnalysisQueue : IAnalysisQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = false });

    public ValueTask EnqueueAsync(Guid runId, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(runId, cancellationToken);

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}

/// <summary>Executes queued analysis runs, and on startup re-queues runs a previous process left unfinished.</summary>
internal sealed class AnalysisWorker(
    IAnalysisQueue queue,
    IServiceScopeFactory scopes,
    IOptions<AnalysisWorkerOptions> options,
    ILogger<AnalysisWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        await RecoverUnfinishedRunsAsync(stoppingToken);
        await Parallel.ForEachAsync(
            queue.DequeueAllAsync(stoppingToken),
            new ParallelOptions { MaxDegreeOfParallelism = options.Value.MaxConcurrency, CancellationToken = stoppingToken },
            async (runId, ct) =>
            {
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    await scope.ServiceProvider.GetRequiredService<RunAnalysis>().ExecuteAsync(runId, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Unhandled error executing analysis run {RunId}", runId);
                }
            });
    }

    private async Task RecoverUnfinishedRunsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var runs = scope.ServiceProvider.GetRequiredService<IAnalysisRunStore>();
        var unfinished = await runs.ListUnfinishedAsync(cancellationToken);
        foreach (var run in unfinished)
        {
            run.Requeue();
        }

        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync(cancellationToken);
        foreach (var run in unfinished)
        {
            await queue.EnqueueAsync(run.Id, cancellationToken);
        }

        if (unfinished.Count > 0)
        {
            logger.LogInformation("Re-queued {Count} unfinished analysis runs", unfinished.Count);
        }
    }
}

/// <summary>Periodically snapshots every user's selected repositories, building score history over time.</summary>
internal sealed class ScheduledReanalysisService(
    IServiceScopeFactory scopes,
    IOptions<AnalysisWorkerOptions> options,
    TimeProvider clock,
    ILogger<ScheduledReanalysisService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var hours = options.Value.ScheduledReanalysisHours;
        if (!options.Value.Enabled || hours <= 0)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(hours), clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ReanalyseAllAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Scheduled re-analysis failed");
            }
        }
    }

    private async Task ReanalyseAllAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var userIds = await scope.ServiceProvider.GetRequiredService<IUserStore>().ListIdsAsync(cancellationToken);
        var request = scope.ServiceProvider.GetRequiredService<RequestAnalysis>();
        var queued = 0;
        foreach (var userId in userIds)
        {
            queued += (await request.ExecuteForSelectedAsync(userId, cancellationToken)).Count;
        }

        logger.LogInformation("Scheduled re-analysis queued {Count} runs for {Users} users", queued, userIds.Count);
    }
}
