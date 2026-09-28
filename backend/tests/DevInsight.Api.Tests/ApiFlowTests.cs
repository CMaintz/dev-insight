using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Web;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevInsight.Api.Tests;

public sealed class ApiFlowTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private HttpClient NewClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    /// <summary>
    /// Walks the real round trip: login (state cookie) → GitHub callback → redirect to the SPA with a
    /// one-time code → code exchanged for a bearer JWT, which the returned client then sends.
    /// </summary>
    private async Task<HttpClient> SignInAsync(string login, string returnUrl = "/dashboard")
    {
        var client = NewClient();
        var start = await client.GetAsync($"/api/auth/github/login?returnUrl={Uri.EscapeDataString(returnUrl)}", Ct);
        start.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var state = HttpUtility.ParseQueryString(start.Headers.Location!.Query)["state"];

        var callback = await client.GetAsync($"/api/auth/github/callback?code={login}&state={state}", Ct);
        callback.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var spa = callback.Headers.Location!;
        spa.GetLeftPart(UriPartial.Path).ShouldBe($"{ApiFactory.FrontendUrl}/auth/callback");
        var query = HttpUtility.ParseQueryString(spa.Query);
        query["returnUrl"].ShouldBe(returnUrl);

        var token = await client.PostAsJsonAsync("/api/auth/exchange", new { code = query["code"] }, Ct).ReadJsonAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token["accessToken"]!.GetValue<string>());
        (await client.PostAsJsonAsync("/api/auth/exchange", new { code = query["code"] }, Ct)).StatusCode
            .ShouldBe(HttpStatusCode.BadRequest, "sign-in codes are single-use");
        return client;
    }

    [Fact]
    public async Task Anonymous_access_is_limited_to_public_endpoints()
    {
        factory.SkipIfUnavailable();
        var client = NewClient();

        (await client.GetAsync("/api/me", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/api/dashboard", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await client.GetAsync("/health", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetAsync("/api/does-not-exist", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.GetAsync("/openapi/v1.json", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Callback_with_a_forged_state_does_not_sign_in()
    {
        factory.SkipIfUnavailable();
        var client = NewClient();
        await client.GetAsync("/api/auth/github/login", Ct);

        var callback = await client.GetAsync("/api/auth/github/callback?code=mallory&state=forged", Ct);

        callback.Headers.Location!.OriginalString.ShouldBe($"{ApiFactory.FrontendUrl}/auth/callback?error=signin_failed");
    }

    [Fact]
    public async Task Cors_allows_only_the_configured_frontend_origin()
    {
        factory.SkipIfUnavailable();
        async Task<string?> PreflightAsync(string origin)
        {
            using var request = new HttpRequestMessage(HttpMethod.Options, "/api/dashboard");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "GET");
            request.Headers.Add("Access-Control-Request-Headers", "authorization");
            var response = await NewClient().SendAsync(request, Ct);
            return response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values) ? values.Single() : null;
        }

        (await PreflightAsync("https://cmaintz.github.io")).ShouldBe("https://cmaintz.github.io");
        (await PreflightAsync("https://evil.example")).ShouldBeNull();
    }

    [Fact]
    public async Task Unknown_sign_in_code_is_rejected()
    {
        factory.SkipIfUnavailable();
        (await NewClient().PostAsJsonAsync("/api/auth/exchange", new { code = "made-up" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Open_redirects_are_neutralised()
    {
        factory.SkipIfUnavailable();
        var client = NewClient();
        var start = await client.GetAsync("/api/auth/github/login?returnUrl=https://evil.example", Ct);
        var state = HttpUtility.ParseQueryString(start.Headers.Location!.Query)["state"];

        var callback = await client.GetAsync($"/api/auth/github/callback?code=eve&state={state}", Ct);

        HttpUtility.ParseQueryString(callback.Headers.Location!.Query)["returnUrl"]
            .ShouldBe("/dashboard");
    }

    [Fact]
    public async Task Bearer_tokens_work_for_api_clients()
    {
        factory.SkipIfUnavailable();
        var browser = await SignInAsync("bearer-user");
        var token = await browser.PostAsync("/api/auth/token", null, Ct).ReadJsonAsync();

        var apiClient = factory.CreateClient();
        (await apiClient.GetAsync("/api/me", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token["accessToken"]!.GetValue<string>());
        (await apiClient.GetAsync("/api/me", Ct).ReadJsonAsync())["login"]!.GetValue<string>().ShouldBe("bearer-user");
    }

    [Fact]
    public async Task Users_cannot_see_each_others_repositories()
    {
        factory.SkipIfUnavailable();
        var alice = await SignInAsync("alice");
        await alice.PostAsync("/api/repos/import", null, Ct);
        var aliceRepo = (await alice.GetAsync("/api/repos", Ct).ReadJsonArrayAsync())[0]!["id"]!.GetValue<string>();

        var bob = await SignInAsync("bob");
        (await bob.GetAsync($"/api/repos/{aliceRepo}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await bob.PostAsync($"/api/analysis/run/{aliceRepo}", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Full_journey_from_sign_in_to_public_portfolio()
    {
        factory.SkipIfUnavailable();
        var client = await SignInAsync("octo");

        // UC2: import is idempotent
        (await client.PostAsync("/api/repos/import", null, Ct).ReadJsonAsync())["imported"]!.GetValue<int>().ShouldBe(2);
        (await client.PostAsync("/api/repos/import", null, Ct).ReadJsonAsync())["imported"]!.GetValue<int>().ShouldBe(0);
        var repos = await client.GetAsync("/api/repos", Ct).ReadJsonArrayAsync();
        repos.Count.ShouldBe(2);
        var repoId = repos[0]!["id"]!.GetValue<string>();

        // UC2.1: deselect the second repository
        var otherId = repos[1]!["id"]!.GetValue<string>();
        var deselected = await client.PatchAsJsonAsync($"/api/repos/{otherId}/select", new { isSelected = false }, Ct).ReadJsonAsync();
        deselected["isSelected"]!.GetValue<bool>().ShouldBeFalse();

        // UC3: queue + background worker, both scopes
        foreach (var scope in new[] { "repo", "user" })
        {
            var queued = await client.PostAsync($"/api/analysis/run/{repoId}?scope={scope}", null, Ct);
            queued.StatusCode.ShouldBe(HttpStatusCode.Accepted);
            var run = await WaitForRunAsync(client, (await queued.ReadJsonAsync())["id"]!.GetValue<string>());
            run["status"]!.GetValue<string>().ShouldBe("succeeded", run.ToJsonString());
        }

        var analysis = await client.GetAsync($"/api/analysis/{repoId}?scope=user", Ct).ReadJsonAsync();
        analysis["scope"]!.GetValue<string>().ShouldBe("userContribution");
        analysis["overallScore"]!.GetValue<int>().ShouldBeInRange(1, 100);
        analysis["metrics"]!.AsArray().ShouldContain(m => m!["name"]!.GetValue<string>() == "total_commits" && m["value"]!.GetValue<double>() == 2);
        analysis["timeline"]!.AsArray().ShouldNotBeEmpty();

        // UC4: at least two feedback items, including commit and structure feedback
        var feedback = await client.GetAsync($"/api/feedback/{analysis["id"]}", Ct).ReadJsonArrayAsync();
        feedback.Count.ShouldBeGreaterThanOrEqualTo(2);
        feedback.Select(f => f!["category"]!.GetValue<string>()).ShouldContain("commitQuality");
        feedback.Select(f => f!["category"]!.GetValue<string>()).ShouldContain("structure");

        // UC5: dashboard aggregates selected repositories only
        var dashboard = await client.GetAsync("/api/dashboard?scope=user", Ct).ReadJsonAsync();
        (dashboard["repositoryCount"]!.GetValue<int>(), dashboard["selectedCount"]!.GetValue<int>(), dashboard["analyzedCount"]!.GetValue<int>())
            .ShouldBe((2, 1, 1));
        dashboard["languages"]!.AsArray()[0]!["language"]!.GetValue<string>().ShouldBe("C#");
        (await client.GetAsync($"/api/analysis/{repoId}/history?scope=repo", Ct).ReadJsonArrayAsync()).Count.ShouldBe(1);

        // Projects
        var project = await client.PostAsJsonAsync("/api/projects",
            new { name = "DevInsight", description = "It analyses code", imageUrls = new[] { "https://img.example.com/a.png" }, linkedRepositoryIds = new[] { repoId, otherId }, sortOrder = 0 }, Ct);
        project.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await client.PostAsJsonAsync("/api/projects", new { name = "", sortOrder = 0 }, Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // UC6: private until published, then public — with deselected repos hidden
        var anonymous = NewClient();
        (await anonymous.GetAsync("/api/portfolio/octo", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.PutAsJsonAsync("/api/me/profile", new { bio = "I build things", linkedInUrl = "https://www.linkedin.com/in/octo", isPortfolioPublic = true }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.PutAsJsonAsync("/api/me/profile", new { linkedInUrl = "https://evil.example", isPortfolioPublic = true }, Ct))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var portfolio = await anonymous.GetAsync("/api/portfolio/octo", Ct).ReadJsonAsync();
        portfolio["owner"]!["bio"]!.GetValue<string>().ShouldBe("I build things");
        portfolio["repositories"]!.AsArray().ShouldHaveSingleItem()!["id"]!.GetValue<string>().ShouldBe(repoId);
        portfolio["projects"]!.AsArray().ShouldHaveSingleItem()!["linkedRepositoryIds"]!.AsArray().Count.ShouldBe(1);
        portfolio["scores"]!["overall"]!.GetValue<int>().ShouldBeInRange(1, 100);
        portfolio["strengths"]!.AsArray().ShouldAllBe(s => s!["title"] != null);

        // Logout is client-side (discard the token); the endpoint just acknowledges it
        (await client.PostAsync("/api/auth/logout", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    private static async Task<JsonNode> WaitForRunAsync(HttpClient client, string runId)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var run = await client.GetAsync($"/api/analysis/runs/{runId}", Ct).ReadJsonAsync();
            if (run["status"]!.GetValue<string>() is "succeeded" or "failed")
            {
                return run;
            }

            await Task.Delay(100, Ct);
        }

        throw new TimeoutException($"Analysis run {runId} did not finish.");
    }
}

internal static class HttpJson
{
    public static async Task<JsonNode> ReadJsonAsync(this Task<HttpResponseMessage> response) =>
        await (await response).ReadJsonAsync();

    public static async Task<JsonNode> ReadJsonAsync(this HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.IsSuccessStatusCode.ShouldBeTrue($"{(int)response.StatusCode}: {body}");
        return JsonNode.Parse(body)!;
    }

    public static async Task<JsonArray> ReadJsonArrayAsync(this Task<HttpResponseMessage> response) =>
        (await response.ReadJsonAsync()).AsArray();

    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web);
}
