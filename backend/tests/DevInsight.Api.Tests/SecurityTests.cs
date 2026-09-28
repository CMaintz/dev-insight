using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Web;

namespace DevInsight.Api.Tests;

/// <summary>Authentication, authorisation and cross-origin rules, through the real HTTP pipeline.</summary>
public sealed class SecurityTests(ApiFactory factory) : ApiTestBase(factory)
{
    [Fact]
    public async Task Anonymous_access_is_limited_to_public_endpoints()
    {
        Factory.SkipIfUnavailable();
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
        Factory.SkipIfUnavailable();
        var client = NewClient();
        await client.GetAsync("/api/auth/github/login", Ct);

        var callback = await client.GetAsync("/api/auth/github/callback?code=mallory&state=forged", Ct);

        callback.Headers.Location!.OriginalString.ShouldBe($"{ApiFactory.FrontendUrl}/auth/callback?error=signin_failed");
    }

    [Fact]
    public async Task Sign_in_codes_are_single_use()
    {
        Factory.SkipIfUnavailable();
        var client = NewClient();
        var code = await ReceiveSignInCodeAsync(client, "once");

        (await client.PostAsJsonAsync("/api/auth/exchange", new { code }, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.PostAsJsonAsync("/api/auth/exchange", new { code }, Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unknown_sign_in_code_is_rejected()
    {
        Factory.SkipIfUnavailable();
        (await NewClient().PostAsJsonAsync("/api/auth/exchange", new { code = "made-up" }, Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Open_redirects_are_neutralised()
    {
        Factory.SkipIfUnavailable();
        var client = NewClient();
        var start = await client.GetAsync("/api/auth/github/login?returnUrl=https://evil.example", Ct);
        var state = HttpUtility.ParseQueryString(start.Headers.Location!.Query)["state"];

        var callback = await client.GetAsync($"/api/auth/github/callback?code=eve&state={state}", Ct);

        HttpUtility.ParseQueryString(callback.Headers.Location!.Query)["returnUrl"].ShouldBe("/dashboard");
    }

    [Fact]
    public async Task Cors_allows_only_the_configured_frontend_origin()
    {
        Factory.SkipIfUnavailable();
        (await AllowedOriginAsync("https://cmaintz.github.io")).ShouldBe("https://cmaintz.github.io");
        (await AllowedOriginAsync("https://evil.example")).ShouldBeNull();
    }

    [Fact]
    public async Task Bearer_tokens_work_for_api_clients()
    {
        Factory.SkipIfUnavailable();
        var token = await (await SignInAsync("bearer-user")).PostAsync("/api/auth/token", null, Ct).ReadJsonAsync();
        var apiClient = Factory.CreateClient();
        (await apiClient.GetAsync("/api/me", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        apiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token["accessToken"]!.GetValue<string>());

        (await apiClient.GetAsync("/api/me", Ct).ReadJsonAsync())["login"]!.GetValue<string>().ShouldBe("bearer-user");
    }

    [Fact]
    public async Task Users_cannot_see_each_others_repositories()
    {
        Factory.SkipIfUnavailable();
        var alice = await SignInAsync("alice");
        await alice.PostAsync("/api/repos/import", null, Ct);
        var aliceRepo = (await alice.GetAsync("/api/repos", Ct).ReadJsonArrayAsync())[0]!.Id();

        var bob = await SignInAsync("bob");

        (await bob.GetAsync($"/api/repos/{aliceRepo}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await bob.PostAsync($"/api/analysis/run/{aliceRepo}", null, Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    /// <summary>The Access-Control-Allow-Origin a CORS preflight from <paramref name="origin"/> gets back, if any.</summary>
    private async Task<string?> AllowedOriginAsync(string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/dashboard");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization");
        var response = await NewClient().SendAsync(request, Ct);
        return response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values) ? values.Single() : null;
    }
}
