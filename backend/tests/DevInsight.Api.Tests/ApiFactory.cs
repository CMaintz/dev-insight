using DevInsight.Application.Abstractions;
using DevInsight.Domain.Analyses.Engine;
using DevInsight.Domain.Repositories;
using DevInsight.Domain.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace DevInsight.Api.Tests;

/// <summary>
/// Boots the real API against a throwaway PostgreSQL database, with GitHub and git replaced by fakes.
/// The database is a Testcontainers container, or — when DEVINSIGHT_TEST_POSTGRES holds a server
/// connection string — a fresh database on that server. Without either, the tests are skipped locally;
/// in CI (CI=true) that is a failure, so the pipeline can never pass by skipping its integration tests.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string ExternalServerVariable = "DEVINSIGHT_TEST_POSTGRES";
    private PostgreSqlContainer? _postgres;
    private string? _connectionString;

    public const string FrontendUrl = "https://cmaintz.github.io/DevInsight";

    public string? UnavailableReason { get; private set; }
    public FakeGitHub GitHub { get; } = new();

    public async ValueTask InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable(ExternalServerVariable) is { Length: > 0 } server)
        {
            _connectionString = $"{server};Database=devinsight_test_{Guid.NewGuid():N}";
            return;
        }

        try
        {
            _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
            await _postgres.StartAsync();
            _connectionString = _postgres.GetConnectionString();
        }
        catch (Exception ex) when (Environment.GetEnvironmentVariable("CI") is null)
        {
            UnavailableReason = $"Docker is not available ({ex.GetType().Name}); integration tests skipped locally.";
        }
    }

    public void SkipIfUnavailable()
    {
        if (UnavailableReason is not null)
        {
            Assert.Skip(UnavailableReason);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DevInsight", _connectionString ?? "Host=unavailable");
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-at-least-32-bytes");
        builder.UseSetting("GitHub:CallbackUrl", "http://localhost/api/auth/github/callback");
        builder.UseSetting("Frontend:Url", FrontendUrl);
        builder.UseSetting("AnalysisWorker:ScheduledReanalysisHours", "0");
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IGitHubGateway>();
            services.AddSingleton<IGitHubGateway>(GitHub);
            services.RemoveAll<IRepositorySnapshotSource>();
            services.AddSingleton<IRepositorySnapshotSource, FakeSnapshots>();
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }
    }
}

/// <summary>GitHub stand-in: every OAuth code maps to an identity, every identity owns two repositories.</summary>
public sealed class FakeGitHub : IGitHubGateway
{
    public Uri GetAuthorizationUrl(string state) => new($"https://github.com/login/oauth/authorize?client_id=test&state={state}");

    public Task<string> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken) => Task.FromResult(code);

    /// <summary>The "token" is the code, and the code is the login.</summary>
    public Task<GitHubIdentity> GetIdentityAsync(string accessToken, CancellationToken cancellationToken) =>
        Task.FromResult(new GitHubIdentity(Math.Abs(accessToken.GetHashCode(StringComparison.Ordinal)), accessToken, accessToken,
            $"{accessToken}@example.com", null, [$"{accessToken}@example.com"]));

    public Task<IReadOnlyList<GitHubRepositoryInfo>> ListRepositoriesAsync(string accessToken, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<GitHubRepositoryInfo>>(
        [
            Info(accessToken, "api", 1),
            Info(accessToken, "web", 2),
        ]);

    public Task<IReadOnlyDictionary<string, long>> GetLanguagesAsync(string accessToken, string owner, string name, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<string, long>>(new Dictionary<string, long> { ["C#"] = 700, ["TypeScript"] = 300 });

    private static GitHubRepositoryInfo Info(string owner, string name, int n) =>
        new(Math.Abs(HashCode.Combine(owner, name)), owner, name, $"The {name}", $"https://github.com/{owner}/{name}",
            "C#", n, 0, false, false, false, "main", 10, DateTimeOffset.UtcNow.AddDays(-n));
}

internal sealed class FakeSnapshots : IRepositorySnapshotSource
{
    public Task<RepositorySnapshot> CaptureAsync(Repository repository, string accessToken, CancellationToken cancellationToken)
    {
        var mine = $"{accessToken}@example.com";
        CommitRecord Commit(string email, string subject, int daysAgo, params string[] paths) =>
            new(Guid.NewGuid().ToString("N"), "dev", email, DateTimeOffset.UtcNow.AddDays(-daysAgo), subject, 40, 10, false, paths);

        return Task.FromResult(new RepositorySnapshot(
            "0123456789abcdef",
            [
                Commit(mine, "feat: add analysis engine", 1, "src/Engine.cs", "tests/EngineTests.cs"),
                Commit(mine, "fix", 8, "src/Engine.cs"),
                Commit("other@example.com", "docs: explain scoring", 15, "README.md"),
            ],
            [
                new SourceFile("README.md", 40),
                new SourceFile(".editorconfig", 12),
                new SourceFile(".github/workflows/ci.yml", 30),
                new SourceFile("src/Engine.cs", 140),
                new SourceFile("src/Program.cs", 900),
                new SourceFile("tests/EngineTests.cs", 90),
            ]));
    }
}
