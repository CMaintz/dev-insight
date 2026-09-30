using DevInsight.Application.Abstractions;
using DevInsight.Application.Common;
using DevInsight.Application.Dashboard;
using DevInsight.Application.Portfolio;
using DevInsight.Domain.Analyses;
using DevInsight.Domain.Projects;

namespace DevInsight.Application.Tests;

public class GetDashboardTests
{
    private readonly World _world = new();

    [Fact]
    public async Task Aggregates_cover_selected_repositories_only()
    {
        var user = _world.AddUser();
        var selected = _world.AddRepository(user, "selected");
        selected.SetLanguages(new Dictionary<string, long> { ["C#"] = 300, ["TypeScript"] = 100 });
        var excluded = _world.AddRepository(user, "excluded");
        excluded.SetLanguages(new Dictionary<string, long> { ["Go"] = 10_000 });
        excluded.SetSelected(false, World.Now);
        var analysis = _world.AddAnalysis(selected, AnalysisScope.Repo, World.Now);
        _world.AddAnalysis(excluded, AnalysisScope.Repo, World.Now);

        var view = await new GetDashboard(_world.Repositories, _world.Analyses)
            .ExecuteAsync(user.Id, AnalysisScope.Repo, TestContext.Current.CancellationToken);

        (view.RepositoryCount, view.SelectedCount, view.AnalyzedCount).ShouldBe((2, 1, 1));
        view.AverageScores!.Overall.ShouldBe(analysis.OverallScore);
        view.Languages.Select(l => (l.Language, l.Share)).ShouldBe([("C#", 0.75), ("TypeScript", 0.25)]);
        view.Repositories.First().Repository.ShouldBe(selected);
        view.TopFeedback.ShouldAllBe(f => f.RepositoryId == selected.Id && !f.Feedback.IsStrength);
    }

    [Fact]
    public async Task Empty_account_has_no_scores()
    {
        var user = _world.AddUser();
        var view = await new GetDashboard(_world.Repositories, _world.Analyses)
            .ExecuteAsync(user.Id, AnalysisScope.Repo, TestContext.Current.CancellationToken);
        (view.AverageScores, view.ScoreEvolution.Count, view.CommitQuality.Commits).ShouldBe((null, 0, 0));
    }
}

public class InsightsTests
{
    private static ScoreSnapshot Snap(Guid repo, int day, int overall) =>
        new(Guid.NewGuid(), repo, World.Now.AddDays(day), overall, overall, overall, overall);

    [Fact]
    public void Score_evolution_carries_each_repositorys_latest_score_forward()
    {
        Guid a = Guid.NewGuid(), b = Guid.NewGuid();
        var points = Insights.ScoreEvolution([Snap(a, 0, 40), Snap(b, 1, 80), Snap(a, 2, 60)]);

        points.Select(p => p.Overall).ShouldBe([40, 60, 70]);
    }
}

public class GetPortfolioTests
{
    private readonly World _world = new();

    private GetPortfolio Sut() => new(_world.Users, _world.Repositories, _world.Analyses, _world.Projects);

    [Fact]
    public async Task Unpublished_portfolio_is_hidden_from_visitors()
    {
        _world.AddUser();
        await Should.ThrowAsync<NotFoundException>(() => Sut().ExecuteAsync("octo", viewerId: null, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Owner_can_preview_an_unpublished_portfolio()
    {
        var owner = _world.AddUser();
        (await Sut().ExecuteAsync("octo", owner.Id, TestContext.Current.CancellationToken)).Owner.ShouldBe(owner);
    }

    [Fact]
    public async Task Public_portfolio_hides_private_or_deselected_repositories()
    {
        var (view, shown) = await PublishedPortfolioWithHiddenRepositoriesAsync();

        view.Repositories.ShouldHaveSingleItem().Repository.ShouldBe(shown);
        view.Projects.ShouldHaveSingleItem().LinkedRepositoryIds.ShouldBe([shown.Id]);
        view.Scores.ShouldNotBeNull();
    }

    [Fact]
    public async Task Public_portfolio_shows_strengths_only()
    {
        var (view, _) = await PublishedPortfolioWithHiddenRepositoriesAsync();

        view.Strengths.ShouldNotBeEmpty();
        view.Strengths.ShouldAllBe(s => s.Feedback.IsStrength);
    }

    /// <summary>A published portfolio with one shown, one private and one deselected repository, all linked from a project.</summary>
    private async Task<(PortfolioView View, Domain.Repositories.Repository Shown)> PublishedPortfolioWithHiddenRepositoriesAsync()
    {
        var owner = _world.AddUser();
        owner.UpdateProfile("Hello", null, isPortfolioPublic: true, World.Now);
        var shown = _world.AddRepository(owner, "shown");
        var privateRepo = _world.AddRepository(owner, "secret", isPrivate: true);
        var deselected = _world.AddRepository(owner, "old");
        deselected.SetSelected(false, World.Now);
        _world.AddAnalysis(shown, AnalysisScope.UserContribution, World.Now);
        _world.Projects.Add(Project.Create(owner.Id, new ProjectDetails("P", null, [], [shown.Id, privateRepo.Id, deselected.Id], 0), World.Now));
        return (await Sut().ExecuteAsync(owner.Id.ToString(), viewerId: null, TestContext.Current.CancellationToken), shown);
    }

    [Fact]
    public async Task Unknown_handle_is_not_found() =>
        await Should.ThrowAsync<NotFoundException>(() => Sut().ExecuteAsync("nobody", null, TestContext.Current.CancellationToken));
}
