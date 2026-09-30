using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace DevInsight.Api.Tests;

/// <summary>
/// The whole product through the real API, one user story per step: sign in → import → select → analyse →
/// feedback → dashboard → projects → publish → public portfolio.
/// </summary>
public sealed class JourneyTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Full_journey_from_sign_in_to_public_portfolio()
    {
        Factory.SkipIfUnavailable();
        var client = await SignInAsync("octo");

        var (shown, hidden) = await ImportTwoRepositoriesAsync(client);
        await DeselectAsync(client, hidden);
        await AnalyseInBothScopesAsync(client, shown);
        var analysis = await AssertUserContributionAnalysisAsync(client, shown);
        await AssertFeedbackCoversCommitsAndStructureAsync(client, analysis);
        await AssertDashboardAggregatesSelectedOnlyAsync(client, shown);
        await CreateProjectLinkingAsync(client, shown, hidden);
        await PublishPortfolioAsync(client);
        await AssertPublicPortfolioShowsOnlySelectedWorkAsync(shown);
    }

    /// <summary>UC2: import twice — the second import updates instead of duplicating.</summary>
    private static async Task<(string First, string Second)> ImportTwoRepositoriesAsync(HttpClient client)
    {
        (await client.PostAsync("/api/repos/import", null, Ct).ReadJsonAsync())["imported"]!.GetValue<int>().ShouldBe(2);
        (await client.PostAsync("/api/repos/import", null, Ct).ReadJsonAsync())["imported"]!.GetValue<int>().ShouldBe(0);
        var repos = await client.GetAsync("/api/repos", Ct).ReadJsonArrayAsync();
        repos.Count.ShouldBe(2);
        return (repos[0]!.Id(), repos[1]!.Id());
    }

    /// <summary>UC2.1</summary>
    private static async Task DeselectAsync(HttpClient client, string repositoryId)
    {
        var repository = await client.PatchAsJsonAsync($"/api/repos/{repositoryId}/select", new { isSelected = false }, Ct).ReadJsonAsync();
        repository["isSelected"]!.GetValue<bool>().ShouldBeFalse();
    }

    /// <summary>UC3: queued runs complete through the background worker.</summary>
    private static async Task AnalyseInBothScopesAsync(HttpClient client, string repositoryId)
    {
        foreach (var scope in new[] { "repo", "user" })
        {
            var queued = await client.PostAsync($"/api/analysis/run/{repositoryId}?scope={scope}", null, Ct);
            queued.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            var run = await WaitForRunAsync(client, (await queued.ReadJsonAsync()).Id());
            run["status"]!.GetValue<string>().ShouldBe("succeeded", run.ToJsonString());
        }
    }

    private static async Task<string> AssertUserContributionAnalysisAsync(HttpClient client, string repositoryId)
    {
        var analysis = await client.GetAsync($"/api/analysis/{repositoryId}?scope=user", Ct).ReadJsonAsync();
        analysis["scope"]!.GetValue<string>().ShouldBe("userContribution");
        analysis["overallScore"]!.GetValue<int>().ShouldBeInRange(1, 100);
        analysis["metrics"]!.AsArray().ShouldContain(m => m!["name"]!.GetValue<string>() == "total_commits" && m["value"]!.GetValue<double>() == 2);
        analysis["timeline"]!.AsArray().ShouldNotBeEmpty();
        return analysis.Id();
    }

    /// <summary>UC4: at least two items, including commit-message and structure feedback.</summary>
    private static async Task AssertFeedbackCoversCommitsAndStructureAsync(HttpClient client, string analysisId)
    {
        var categories = (await client.GetAsync($"/api/feedback/{analysisId}", Ct).ReadJsonArrayAsync())
            .Select(f => f!["category"]!.GetValue<string>()).ToList();
        categories.Count.ShouldBeGreaterThanOrEqualTo(2);
        categories.ShouldContain("commitQuality");
        categories.ShouldContain("structure");
    }

    /// <summary>UC5</summary>
    private static async Task AssertDashboardAggregatesSelectedOnlyAsync(HttpClient client, string repositoryId)
    {
        var dashboard = await client.GetAsync("/api/dashboard?scope=user", Ct).ReadJsonAsync();
        (Count(dashboard, "repositoryCount"), Count(dashboard, "selectedCount"), Count(dashboard, "analyzedCount")).ShouldBe((2, 1, 1));
        dashboard["languages"]!.AsArray()[0]!["language"]!.GetValue<string>().ShouldBe("C#");
        (await client.GetAsync($"/api/analysis/{repositoryId}/history?scope=repo", Ct).ReadJsonArrayAsync()).Count.ShouldBe(1);
    }

    private static async Task CreateProjectLinkingAsync(HttpClient client, params string[] repositoryIds)
    {
        var project = new { name = "DevInsight", description = "It analyses code", imageUrls = new[] { "https://img.example.com/a.png" }, linkedRepositoryIds = repositoryIds, sortOrder = 0 };
        (await client.PostAsJsonAsync("/api/projects", project, Ct)).StatusCode.ShouldBe(HttpStatusCode.Created);
        (await client.PostAsJsonAsync("/api/projects", new { name = "", sortOrder = 0 }, Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    /// <summary>UC6: private until published; the profile validates the LinkedIn URL.</summary>
    private async Task PublishPortfolioAsync(HttpClient client)
    {
        (await NewClient().GetAsync("/api/portfolio/octo", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var profile = new { bio = "I build things", linkedInUrl = "https://www.linkedin.com/in/octo", isPortfolioPublic = true };
        (await client.PutAsJsonAsync("/api/me/profile", profile, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        var invalid = new { linkedInUrl = "https://evil.example", isPortfolioPublic = true };
        (await client.PutAsJsonAsync("/api/me/profile", invalid, Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task AssertPublicPortfolioShowsOnlySelectedWorkAsync(string shownRepositoryId)
    {
        var portfolio = await NewClient().GetAsync("/api/portfolio/octo", Ct).ReadJsonAsync();
        portfolio["owner"]!["bio"]!.GetValue<string>().ShouldBe("I build things");
        portfolio["repositories"]!.AsArray().ShouldHaveSingleItem()!.Id().ShouldBe(shownRepositoryId);
        portfolio["projects"]!.AsArray().ShouldHaveSingleItem()!["linkedRepositoryIds"]!.AsArray().Count.ShouldBe(1);
        portfolio["scores"]!["overall"]!.GetValue<int>().ShouldBeInRange(1, 100);
        portfolio["strengths"]!.AsArray().ShouldAllBe(s => s!["title"] != null);
    }

    private static int Count(JsonNode node, string property) => node[property]!.GetValue<int>();
}
