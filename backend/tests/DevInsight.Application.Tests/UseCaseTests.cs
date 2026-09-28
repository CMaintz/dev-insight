using DevInsight.Application.Analyses;
using DevInsight.Application.Auth;
using DevInsight.Application.Common;
using DevInsight.Application.Projects;
using DevInsight.Application.Repositories;
using DevInsight.Domain.Analyses;
using DevInsight.Domain.Common;
using DevInsight.Domain.Projects;
using Microsoft.Extensions.Logging.Abstractions;

namespace DevInsight.Application.Tests;

public class LoginWithGitHubTests
{
    private readonly World _world = new();

    private LoginWithGitHub Sut() => new(_world.GitHub, _world.Users, _world.Credentials, _world, _world.Clock);

    [Fact]
    public async Task First_login_registers_the_user_with_their_token()
    {
        var user = await Sut().ExecuteAsync("code1", TestContext.Current.CancellationToken);

        _world.Users.All.ShouldHaveSingleItem().ShouldBe(user);
        user.GitHubId.ShouldBe(42);
        _world.Credentials.Tokens[user.Id].ShouldBe("token-for-code1");
    }

    [Fact]
    public async Task Repeat_login_updates_the_existing_user()
    {
        var first = await Sut().ExecuteAsync("code1", TestContext.Current.CancellationToken);
        _world.GitHub.Identity = _world.GitHub.Identity with { Login = "octo-renamed" };

        var second = await Sut().ExecuteAsync("code2", TestContext.Current.CancellationToken);

        second.Id.ShouldBe(first.Id);
        _world.Users.All.Count.ShouldBe(1);
        second.Login.ShouldBe("octo-renamed");
        _world.Credentials.Tokens[second.Id].ShouldBe("token-for-code2");
    }
}

public class ImportRepositoriesTests
{
    private readonly World _world = new();

    private ImportRepositories Sut() =>
        new(_world.GitHub, _world.Users, _world.Credentials, _world.Repositories, _world, _world.Clock);

    [Fact]
    public async Task Reimport_updates_instead_of_duplicating()
    {
        var user = _world.AddUser();
        _world.GitHub.Repositories.Add(FakeGitHub.Info("octo", "a", id: 1));
        _world.GitHub.Repositories.Add(FakeGitHub.Info("octo", "b", id: 2));

        var first = await Sut().ExecuteAsync(user.Id, TestContext.Current.CancellationToken);
        _world.GitHub.Repositories[0] = FakeGitHub.Info("octo", "a", id: 1, stars: 99);
        var second = await Sut().ExecuteAsync(user.Id, TestContext.Current.CancellationToken);

        first.ShouldBe(new ImportSummary(2, 0, 2));
        second.ShouldBe(new ImportSummary(0, 2, 2));
        _world.Repositories.All.Count.ShouldBe(2);
        _world.Repositories.All.Single(r => r.GitHubRepoId == 1).Stars.ShouldBe(99);
        _world.Repositories.All.ShouldAllBe(r => r.Languages["C#"] == 900);
    }

    [Fact]
    public async Task Import_without_a_token_asks_to_sign_in_again()
    {
        var user = _world.AddUser();
        _world.Credentials.Tokens.Clear();
        await Should.ThrowAsync<PreconditionFailedException>(() => Sut().ExecuteAsync(user.Id, TestContext.Current.CancellationToken));
    }
}

public class RepositoryQueriesTests
{
    private readonly World _world = new();

    [Fact]
    public async Task Another_users_repository_is_not_found()
    {
        var owner = _world.AddUser("owner", 1);
        var intruder = _world.AddUser("intruder", 2);
        var repository = _world.AddRepository(owner);
        var sut = new RepositoryQueries(_world.Repositories, _world, _world.Clock);

        await Should.ThrowAsync<NotFoundException>(() => sut.GetAsync(intruder.Id, repository.Id, TestContext.Current.CancellationToken));
        await Should.ThrowAsync<NotFoundException>(() => sut.SetSelectedAsync(intruder.Id, repository.Id, false, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Selection_change_is_saved()
    {
        var owner = _world.AddUser();
        var repository = _world.AddRepository(owner);
        var sut = new RepositoryQueries(_world.Repositories, _world, _world.Clock);

        (await sut.SetSelectedAsync(owner.Id, repository.Id, false, TestContext.Current.CancellationToken)).IsSelected.ShouldBeFalse();
        _world.Saves.ShouldBe(1);
    }
}

public class AnalysisUseCaseTests
{
    private readonly World _world = new();

    private RequestAnalysis Request() =>
        new(new RepositoryQueries(_world.Repositories, _world, _world.Clock), _world.Runs, _world.Queue, _world, _world.Clock);

    private RunAnalysis Run() =>
        new(_world.Runs, _world.Repositories, _world.Users, _world.Analyses, _world.Credentials, _world.Snapshots,
            _world.Ai, _world, _world.Clock, NullLogger<RunAnalysis>.Instance);

    [Fact]
    public async Task Requesting_twice_returns_the_pending_run()
    {
        var user = _world.AddUser();
        var repository = _world.AddRepository(user);

        var first = await Request().ExecuteAsync(user.Id, repository.Id, AnalysisScope.Repo, TestContext.Current.CancellationToken);
        var second = await Request().ExecuteAsync(user.Id, repository.Id, AnalysisScope.Repo, TestContext.Current.CancellationToken);

        second.Id.ShouldBe(first.Id);
        _world.Queue.Enqueued.ShouldBe([first.Id]);
    }

    [Fact]
    public async Task Analyse_all_queues_both_scopes_of_selected_repositories()
    {
        var user = _world.AddUser();
        _world.AddRepository(user, "a");
        _world.AddRepository(user, "b").SetSelected(false, World.Now);

        var runs = await Request().ExecuteForSelectedAsync(user.Id, TestContext.Current.CancellationToken);

        runs.Select(r => r.Scope).ShouldBe([AnalysisScope.Repo, AnalysisScope.UserContribution]);
    }

    [Fact]
    public async Task Successful_run_stores_a_scored_analysis_with_feedback()
    {
        var user = _world.AddUser();
        var repository = _world.AddRepository(user);
        var run = await Request().ExecuteAsync(user.Id, repository.Id, AnalysisScope.UserContribution, TestContext.Current.CancellationToken);

        await Run().ExecuteAsync(run.Id, TestContext.Current.CancellationToken);

        run.Status.ShouldBe(AnalysisRunStatus.Succeeded);
        var analysis = _world.Analyses.All.ShouldHaveSingleItem();
        run.AnalysisId.ShouldBe(analysis.Id);
        analysis.MetricValue(MetricKeys.TotalCommits).ShouldBe(1); // only the user's commit
        analysis.Feedback.Count.ShouldBeGreaterThanOrEqualTo(2);
        analysis.Feedback.ShouldAllBe(f => f.Type == FeedbackType.RuleBased);
    }

    [Fact]
    public async Task Failed_capture_marks_the_run_failed()
    {
        var user = _world.AddUser();
        var repository = _world.AddRepository(user);
        _world.Snapshots.Failure = new InvalidOperationException("clone failed");
        var run = await Request().ExecuteAsync(user.Id, repository.Id, AnalysisScope.Repo, TestContext.Current.CancellationToken);

        await Run().ExecuteAsync(run.Id, TestContext.Current.CancellationToken);

        (run.Status, run.Error).ShouldBe((AnalysisRunStatus.Failed, "clone failed"));
        _world.Analyses.All.ShouldBeEmpty();
    }

    [Fact]
    public async Task Ai_feedback_is_added_when_enabled()
    {
        _world.Ai.IsEnabled = true;

        await AnalyseNewRepositoryAsync();

        _world.Analyses.All.Single().Feedback.ShouldContain(f => f.Type == FeedbackType.Ai && f.Title == "Prioritise tests");
    }

    [Fact]
    public async Task Failing_ai_feedback_does_not_fail_the_analysis()
    {
        _world.Ai.IsEnabled = true;
        _world.Ai.Failure = new HttpRequestException("down");

        var run = await AnalyseNewRepositoryAsync();

        run.Status.ShouldBe(AnalysisRunStatus.Succeeded);
        _world.Analyses.All.Single().Feedback.ShouldNotContain(f => f.Type == FeedbackType.Ai);
    }

    private async Task<AnalysisRun> AnalyseNewRepositoryAsync()
    {
        var user = _world.AddUser();
        var repository = _world.AddRepository(user);
        var run = await Request().ExecuteAsync(user.Id, repository.Id, AnalysisScope.Repo, TestContext.Current.CancellationToken);
        await Run().ExecuteAsync(run.Id, TestContext.Current.CancellationToken);
        return run;
    }

    [Fact]
    public async Task Run_that_is_not_queued_is_ignored()
    {
        await Run().ExecuteAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
        _world.Saves.ShouldBe(0);
    }
}

public class ProjectUseCaseTests
{
    private readonly World _world = new();

    [Fact]
    public async Task Projects_cannot_link_other_users_repositories()
    {
        var user = _world.AddUser("me", 1);
        var foreign = _world.AddRepository(_world.AddUser("them", 2));
        var sut = new ProjectUseCases(_world.Projects, _world.Repositories, _world, _world.Clock);

        await Should.ThrowAsync<DomainException>(() => sut.CreateAsync(
            user.Id, new ProjectDetails("P", null, [], [foreign.Id], 0), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Project_crud_round_trip()
    {
        var user = _world.AddUser();
        var repository = _world.AddRepository(user);
        var sut = new ProjectUseCases(_world.Projects, _world.Repositories, _world, _world.Clock);
        var ct = TestContext.Current.CancellationToken;

        var project = await sut.CreateAsync(user.Id, new ProjectDetails("P", null, [], [repository.Id], 0), ct);
        await sut.UpdateAsync(user.Id, project.Id, new ProjectDetails("Renamed", "d", [], [], 1), ct);
        (await sut.ListAsync(user.Id, ct)).ShouldHaveSingleItem().Name.ShouldBe("Renamed");
        await sut.DeleteAsync(user.Id, project.Id, ct);
        (await sut.ListAsync(user.Id, ct)).ShouldBeEmpty();
    }
}
