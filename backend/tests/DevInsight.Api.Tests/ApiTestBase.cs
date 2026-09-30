using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Web;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DevInsight.Api.Tests;

/// <summary>Shared HTTP helpers: clients, the real sign-in round trip, and run polling.</summary>
public abstract class ApiTestBase(ApiFactory factory) : IClassFixture<ApiFactory>
{
    protected ApiFactory Factory { get; } = factory;

    protected static CancellationToken Ct => TestContext.Current.CancellationToken;

    protected HttpClient NewClient() =>
        Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    /// <summary>Signs in as <paramref name="login"/> and returns a client that sends the resulting bearer JWT.</summary>
    protected async Task<HttpClient> SignInAsync(string login)
    {
        var client = NewClient();
        var token = await client.PostAsJsonAsync("/api/auth/exchange", new { code = await ReceiveSignInCodeAsync(client, login) }, Ct).ReadJsonAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token["accessToken"]!.GetValue<string>());
        return client;
    }

    /// <summary>
    /// The real round trip up to the SPA: login (state cookie) → GitHub callback → redirect to the SPA's
    /// callback page with a one-time code, which is returned.
    /// </summary>
    protected static async Task<string> ReceiveSignInCodeAsync(HttpClient client, string login, string returnUrl = "/dashboard")
    {
        var start = await client.GetAsync($"/api/auth/github/login?returnUrl={Uri.EscapeDataString(returnUrl)}", Ct);
        start.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var state = HttpUtility.ParseQueryString(start.Headers.Location!.Query)["state"];

        var callback = await client.GetAsync($"/api/auth/github/callback?code={login}&state={state}", Ct);
        callback.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        callback.Headers.Location!.GetLeftPart(UriPartial.Path).ShouldBe($"{ApiFactory.FrontendUrl}/auth/callback");
        var query = HttpUtility.ParseQueryString(callback.Headers.Location.Query);
        query["returnUrl"].ShouldBe(returnUrl);
        return query["code"]!;
    }

    protected static async Task<JsonNode> WaitForRunAsync(HttpClient client, string runId)
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

    public static string Id(this JsonNode node) => node["id"]!.GetValue<string>();
}
