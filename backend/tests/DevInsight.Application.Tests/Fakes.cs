using DevInsight.Application.Abstractions;
using DevInsight.Domain.Analyses;
using DevInsight.Domain.Analyses.Engine;
using DevInsight.Domain.Projects;
using DevInsight.Domain.Repositories;
using DevInsight.Domain.Users;

namespace DevInsight.Application.Tests;

/// <summary>In-memory adapters for every port — the application layer tested without a database or network.</summary>
internal sealed class World : IUnitOfWork
{
    public static readonly DateTimeOffset Now = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    public FakeTime Clock { get; } = new(Now);
    public int Saves { get; private set; }
    public InMemoryUsers Users { get; } = new();
    public InMemoryRepositories Repositories { get; } = new();
    public InMemoryAnalyses Analyses { get; } = new();
    public InMemoryRuns Runs { get; } = new();
    public InMemoryProjects Projects { get; } = new();
    public InMemoryCredentials Credentials { get; } = new();
    public FakeGitHub GitHub { get; } = new();
    public FakeSnapshots Snapshots { get; } = new();
    public FakeAi Ai { get; } = new();
    public RecordingQueue Queue { get; } = new();

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        Saves++;
        return Task.CompletedTask;
    }

    public User AddUser(string login = "octo", long gitHubId = 42)
    {
        var user = User.Register(new GitHubIdentity(gitHubId, login, login, $"{login}@example.com", null, []), Now);
        Users.Add(user);
        Credentials.Tokens[user.Id] = $"token-{login}";
        return user;
    }

    public Repository AddRepository(User owner, string name = "repo", bool isPrivate = false, long? gitHubId = null)
    {
        var repository = Repository.Import(owner.Id, owner.Login, FakeGitHub.Info(owner.Login, name, isPrivate, gitHubId), Now);
        Repositories.Add(repository);
        return repository;
    }

    public Analysis AddAnalysis(Repository repository, AnalysisScope scope, DateTimeOffset at, IReadOnlyList<CommitRecord>? commits = null)
    {
        var snapshot = new RepositorySnapshot("sha", commits ?? [FakeSnapshots.Commit("octo@example.com")], FakeSnapshots.Files);
        var result = AnalysisEngine.Analyze(snapshot, scope, new ContributorIdentity("octo", ["octo@example.com"]), at);
        var analysis = Analysis.Create(repository.Id, scope, result, at);
        analysis.AddFeedback(Domain.Analyses.Rules.FeedbackRuleSet.Default.Evaluate(analysis, at));
        Analyses.Add(analysis);
        return analysis;
    }
}

internal sealed class FakeTime(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Current { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Current;
}

internal sealed class InMemoryUsers : IUserStore
{
    public List<User> All { get; } = [];

    public Task<User?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(All.FirstOrDefault(u => u.Id == id));

    public Task<User?> FindByGitHubIdAsync(long gitHubId, CancellationToken ct) =>
        Task.FromResult(All.FirstOrDefault(u => u.GitHubId == gitHubId));

    public Task<User?> FindByLoginAsync(string login, CancellationToken ct) =>
        Task.FromResult(All.FirstOrDefault(u => string.Equals(u.Login, login, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<Guid>> ListIdsAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<Guid>>([.. All.Select(u => u.Id)]);

    public void Add(User user) => All.Add(user);
}

internal sealed class InMemoryRepositories : IRepositoryStore
{
    public List<Repository> All { get; } = [];

    public Task<Repository?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(All.FirstOrDefault(r => r.Id == id));

    public Task<IReadOnlyList<Repository>> ListForUserAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Repository>>([.. All.Where(r => r.UserId == userId)]);

    public void Add(Repository repository) => All.Add(repository);
}

internal sealed class InMemoryAnalyses : IAnalysisStore
{
    public List<Analysis> All { get; } = [];

    public Task<Analysis?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(All.FirstOrDefault(a => a.Id == id));

    public Task<Analysis?> GetLatestAsync(Guid repositoryId, AnalysisScope scope, CancellationToken ct) =>
        Task.FromResult(All.Where(a => a.RepositoryId == repositoryId && a.Scope == scope).MaxBy(a => a.CreatedAt));

    public Task<IReadOnlyList<Analysis>> GetLatestForRepositoriesAsync(IReadOnlyCollection<Guid> ids, AnalysisScope scope, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Analysis>>([.. All.Where(a => ids.Contains(a.RepositoryId) && a.Scope == scope)
            .GroupBy(a => a.RepositoryId).Select(g => g.MaxBy(a => a.CreatedAt)!)]);

    public Task<IReadOnlyList<ScoreSnapshot>> GetScoreHistoryAsync(IReadOnlyCollection<Guid> ids, AnalysisScope scope, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ScoreSnapshot>>([.. All.Where(a => ids.Contains(a.RepositoryId) && a.Scope == scope)
            .OrderBy(a => a.CreatedAt)
            .Select(a => new ScoreSnapshot(a.Id, a.RepositoryId, a.CreatedAt, a.OverallScore, a.ActivityScore, a.StructureScore, a.QualityScore))]);

    public void Add(Analysis analysis) => All.Add(analysis);
}

internal sealed class InMemoryRuns : IAnalysisRunStore
{
    public List<AnalysisRun> All { get; } = [];

    public Task<AnalysisRun?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(All.FirstOrDefault(r => r.Id == id));

    public Task<AnalysisRun?> FindUnfinishedAsync(Guid repositoryId, AnalysisScope scope, CancellationToken ct) =>
        Task.FromResult(All.FirstOrDefault(r => r.RepositoryId == repositoryId && r.Scope == scope && !r.IsFinished));

    public Task<IReadOnlyList<AnalysisRun>> ListUnfinishedAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<AnalysisRun>>([.. All.Where(r => !r.IsFinished)]);

    public Task<IReadOnlyList<AnalysisRun>> ListRecentForUserAsync(Guid userId, int limit, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<AnalysisRun>>([.. All.Where(r => r.UserId == userId).Take(limit)]);

    public void Add(AnalysisRun run) => All.Add(run);
}

internal sealed class InMemoryProjects : IProjectStore
{
    public List<Project> All { get; } = [];

    public Task<Project?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult(All.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<Project>> ListForUserAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Project>>([.. All.Where(p => p.UserId == userId)]);

    public void Add(Project project) => All.Add(project);

    public void Remove(Project project) => All.Remove(project);
}

internal sealed class InMemoryCredentials : IGitHubCredentialStore
{
    public Dictionary<Guid, string> Tokens { get; } = [];

    public Task SaveAsync(Guid userId, string accessToken, CancellationToken ct)
    {
        Tokens[userId] = accessToken;
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(Guid userId, CancellationToken ct) => Task.FromResult(Tokens.GetValueOrDefault(userId));
}

internal sealed class FakeGitHub : IGitHubGateway
{
    public GitHubIdentity Identity { get; set; } = new(42, "octo", "Octo", "octo@example.com", null, ["octo@example.com"]);
    public List<GitHubRepositoryInfo> Repositories { get; } = [];

    public static GitHubRepositoryInfo Info(string owner, string name, bool isPrivate = false, long? id = null, int stars = 1) =>
        new(id ?? Math.Abs(HashCode.Combine(owner, name)), owner, name, $"{name} description", $"https://github.com/{owner}/{name}",
            "C#", stars, 0, false, isPrivate, false, "main", 100, World.Now.AddDays(-1));

    public Uri GetAuthorizationUrl(string state) => new($"https://github.com/login/oauth/authorize?state={state}");

    public Task<string> ExchangeCodeForTokenAsync(string code, CancellationToken ct) => Task.FromResult($"token-for-{code}");

    public Task<GitHubIdentity> GetIdentityAsync(string accessToken, CancellationToken ct) => Task.FromResult(Identity);

    public Task<IReadOnlyList<GitHubRepositoryInfo>> ListRepositoriesAsync(string accessToken, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<GitHubRepositoryInfo>>([.. Repositories]);

    public Task<IReadOnlyDictionary<string, long>> GetLanguagesAsync(string accessToken, string owner, string name, CancellationToken ct) =>
        Task.FromResult<IReadOnlyDictionary<string, long>>(new Dictionary<string, long> { ["C#"] = 900, ["HTML"] = 100 });
}

internal sealed class FakeSnapshots : IRepositorySnapshotSource
{
    public static readonly IReadOnlyList<SourceFile> Files =
    [
        new("README.md", 50), new(".editorconfig", 10), new(".github/workflows/ci.yml", 30),
        new("src/App/Service.cs", 120), new("src/App/Model.cs", 60), new("tests/App.Tests/ServiceTests.cs", 80),
    ];

    public Exception? Failure { get; set; }

    public static CommitRecord Commit(string email, string subject = "feat: add service", int daysAgo = 1) =>
        new(Guid.NewGuid().ToString("N"), "x", email, World.Now.AddDays(-daysAgo), subject, 10, 2, false, ["src/App/Service.cs"]);

    public Task<RepositorySnapshot> CaptureAsync(Repository repository, string accessToken, CancellationToken ct) =>
        Failure is not null
            ? Task.FromException<RepositorySnapshot>(Failure)
            : Task.FromResult(new RepositorySnapshot("head", [Commit("octo@example.com"), Commit("other@example.com")], Files));
}

internal sealed class FakeAi : IAiFeedbackGenerator
{
    public bool IsEnabled { get; set; }
    public Exception? Failure { get; set; }

    public Task<IReadOnlyList<FeedbackFinding>> GenerateAsync(AiFeedbackRequest request, CancellationToken ct) =>
        Failure is not null
            ? Task.FromException<IReadOnlyList<FeedbackFinding>>(Failure)
            : Task.FromResult<IReadOnlyList<FeedbackFinding>>(
                [new FeedbackFinding("ai", MetricCategory.Quality, Severity.Medium, "Prioritise tests", "Start with Service.cs.")]);
}

internal sealed class RecordingQueue : IAnalysisQueue
{
    public List<Guid> Enqueued { get; } = [];

    public ValueTask EnqueueAsync(Guid runId, CancellationToken ct)
    {
        Enqueued.Add(runId);
        return ValueTask.CompletedTask;
    }

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken ct) => Enqueued.ToAsyncEnumerable();
}
