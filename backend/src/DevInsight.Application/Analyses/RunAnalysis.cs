using DevInsight.Application.Abstractions;
using DevInsight.Domain.Analyses;
using DevInsight.Domain.Analyses.Engine;
using DevInsight.Domain.Analyses.Rules;
using Microsoft.Extensions.Logging;

namespace DevInsight.Application.Analyses;

/// <summary>
/// UC3 + UC4 (execution): capture the repository, compute metrics and scores, attach rule-based
/// (and, when configured, AI) feedback, and store the analysis. Called by the background worker.
/// </summary>
public sealed class RunAnalysis(
    IAnalysisRunStore runs,
    IRepositoryStore repositories,
    IUserStore users,
    IAnalysisStore analyses,
    IGitHubCredentialStore credentials,
    IRepositorySnapshotSource snapshots,
    IAiFeedbackGenerator aiFeedback,
    IUnitOfWork unitOfWork,
    TimeProvider clock,
    ILogger<RunAnalysis> logger)
{
    public async Task ExecuteAsync(Guid runId, CancellationToken cancellationToken)
    {
        var run = await runs.GetAsync(runId, cancellationToken);
        if (run is null || run.Status != AnalysisRunStatus.Queued)
        {
            return;
        }

        run.Start(clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var analysis = await AnalyzeAsync(run, cancellationToken);
            run.Succeed(analysis.Id, clock.GetUtcNow());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Analysis run {RunId} failed", run.Id);
            run.Fail(ex.Message, clock.GetUtcNow());
        }

        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<Analysis> AnalyzeAsync(AnalysisRun run, CancellationToken cancellationToken)
    {
        var repository = await repositories.GetAsync(run.RepositoryId, cancellationToken)
            ?? throw new InvalidOperationException("The repository was deleted.");
        var user = await users.GetAsync(run.UserId, cancellationToken)
            ?? throw new InvalidOperationException("The user was deleted.");
        var token = await credentials.GetAsync(user.Id, cancellationToken)
            ?? throw new InvalidOperationException("No GitHub token on file. Sign in with GitHub again.");

        var snapshot = await snapshots.CaptureAsync(repository, token, cancellationToken);
        var now = clock.GetUtcNow();
        var result = AnalysisEngine.Analyze(snapshot, run.Scope, user.ToContributorIdentity(), now);
        var analysis = Analysis.Create(repository.Id, run.Scope, result, now);
        var ruleFeedback = FeedbackRuleSet.Default.Evaluate(analysis, now);
        analysis.AddFeedback(ruleFeedback);
        analysis.AddFeedback(await GenerateAiFeedbackAsync(repository, analysis, ruleFeedback, now, cancellationToken));

        analyses.Add(analysis);
        return analysis;
    }

    /// <summary>AI feedback is a supplement: if it fails, the rule-based analysis still stands.</summary>
    private async Task<IEnumerable<Feedback>> GenerateAiFeedbackAsync(
        Domain.Repositories.Repository repository,
        Analysis analysis,
        IReadOnlyList<Feedback> ruleFeedback,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!aiFeedback.IsEnabled)
        {
            return [];
        }

        try
        {
            var request = new AiFeedbackRequest(repository.FullName, repository.PrimaryLanguage, analysis, ruleFeedback);
            var findings = await aiFeedback.GenerateAsync(request, cancellationToken);
            return findings.Select(f => Feedback.Create(FeedbackType.Ai, f, now));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "AI feedback failed for analysis of {Repository}; keeping rule-based feedback only",
                repository.FullName);
            return [];
        }
    }
}
