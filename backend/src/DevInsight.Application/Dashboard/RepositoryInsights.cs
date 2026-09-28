using DevInsight.Application.Abstractions;
using DevInsight.Domain.Analyses;
using DevInsight.Domain.Repositories;

namespace DevInsight.Application.Dashboard;

/// <summary>
/// A set of repositories with their latest analysis and score history in one scope — the data both the
/// dashboard and the public portfolio aggregate.
/// </summary>
public sealed class RepositoryInsights
{
    private readonly Dictionary<Guid, Analysis> _latestByRepository;

    private RepositoryInsights(IReadOnlyList<Repository> repositories, IReadOnlyList<Analysis> latest, IReadOnlyList<ScoreSnapshot> history)
    {
        Repositories = repositories;
        Latest = latest;
        History = history;
        _latestByRepository = latest.ToDictionary(a => a.RepositoryId);
    }

    public IReadOnlyList<Repository> Repositories { get; }
    public IReadOnlyList<Analysis> Latest { get; }
    public IReadOnlyList<ScoreSnapshot> History { get; }

    public static async Task<RepositoryInsights> LoadAsync(
        IAnalysisStore analyses, IReadOnlyList<Repository> repositories, AnalysisScope scope, CancellationToken cancellationToken)
    {
        List<Guid> ids = [.. repositories.Select(r => r.Id)];
        var latest = await analyses.GetLatestForRepositoriesAsync(ids, scope, cancellationToken);
        var history = await analyses.GetScoreHistoryAsync(ids, scope, cancellationToken);
        return new RepositoryInsights(repositories, latest, history);
    }

    /// <summary>The same insights restricted to the repositories matching <paramref name="include"/>.</summary>
    public RepositoryInsights Where(Func<Repository, bool> include)
    {
        var ids = Repositories.Where(include).Select(r => r.Id).ToHashSet();
        return new RepositoryInsights(
            [.. Repositories.Where(r => ids.Contains(r.Id))],
            [.. Latest.Where(a => ids.Contains(a.RepositoryId))],
            [.. History.Where(s => ids.Contains(s.RepositoryId))]);
    }

    public RepositorySummary SummaryOf(Repository repository) =>
        new(repository, _latestByRepository.GetValueOrDefault(repository.Id));

    /// <summary>Feedback of the latest analyses that matches <paramref name="include"/>, labelled with its repository.</summary>
    public IEnumerable<FeedbackHighlight> Highlights(Func<Feedback, bool> include)
    {
        var names = Repositories.ToDictionary(r => r.Id, r => r.Name);
        return Latest.SelectMany(a => a.Feedback.Where(include).Select(f => new FeedbackHighlight(a.RepositoryId, names[a.RepositoryId], f)));
    }
}
