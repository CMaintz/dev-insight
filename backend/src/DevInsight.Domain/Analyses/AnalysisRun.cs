using DevInsight.Domain.Common;

namespace DevInsight.Domain.Analyses;

public enum AnalysisRunStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
}

/// <summary>
/// A requested analysis. Cloning and measuring a repository takes seconds to minutes, so the API
/// accepts the request, returns this run, and a background worker executes it.
/// </summary>
public sealed class AnalysisRun : IUserOwned
{
    private const int MaxErrorLength = 1000;

    private AnalysisRun() { }

    public Guid Id { get; private set; }
    public Guid RepositoryId { get; private set; }
    public Guid UserId { get; private set; }
    public AnalysisScope Scope { get; private set; }
    public AnalysisRunStatus Status { get; private set; }
    public Guid? AnalysisId { get; private set; }
    public string? Error { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public bool IsFinished => Status is AnalysisRunStatus.Succeeded or AnalysisRunStatus.Failed;

    public static AnalysisRun Queue(Guid userId, Guid repositoryId, AnalysisScope scope, DateTimeOffset now) => new()
    {
        Id = Ids.New(),
        UserId = userId,
        RepositoryId = repositoryId,
        Scope = scope,
        Status = AnalysisRunStatus.Queued,
        RequestedAt = now,
    };

    public void Start(DateTimeOffset now)
    {
        if (Status != AnalysisRunStatus.Queued)
        {
            throw new DomainException($"Only a queued run can start (run {Id} is {Status}).");
        }

        Status = AnalysisRunStatus.Running;
        StartedAt = now;
    }

    public void Succeed(Guid analysisId, DateTimeOffset now)
    {
        Status = AnalysisRunStatus.Succeeded;
        AnalysisId = analysisId;
        CompletedAt = now;
    }

    public void Fail(string error, DateTimeOffset now)
    {
        Status = AnalysisRunStatus.Failed;
        Error = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
        CompletedAt = now;
    }

    /// <summary>A run interrupted by a restart goes back to the queue.</summary>
    public void Requeue()
    {
        Status = AnalysisRunStatus.Queued;
        StartedAt = null;
    }
}
