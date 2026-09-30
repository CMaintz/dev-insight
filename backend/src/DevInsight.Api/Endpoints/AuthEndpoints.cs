using DevInsight.Api.Auth;
using DevInsight.Api.Contracts;
using DevInsight.Application.Abstractions;
using DevInsight.Application.Auth;
using DevInsight.Application.Profile;
using Microsoft.Extensions.Options;

namespace DevInsight.Api.Endpoints;

internal static class AuthEndpoints
{
    public const string ExchangeRateLimit = "exchange";

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");
        auth.MapGet("/github/login", StartGitHubSignIn).WithSummary("Start GitHub sign-in (redirects to GitHub).");
        auth.MapGet("/github/callback", CompleteGitHubSignInAsync)
            .WithSummary("GitHub OAuth callback: creates or updates the user, then redirects to the SPA with a one-time code.");
        auth.MapPost("/exchange", ExchangeSignInCodeAsync)
            .RequireRateLimiting(ExchangeRateLimit).WithSummary("Exchange the one-time sign-in code for a bearer JWT.");
        auth.MapPost("/logout", () => Results.NoContent())
            .WithSummary("End the session. Tokens are stateless; the client discards its token.");
        auth.MapPost("/token", IssueTokenAsync).RequireAuthorization().WithSummary("Issue a fresh bearer JWT for the signed-in user.");
    }

    private static IResult StartGitHubSignIn(HttpContext context, OAuthState oauth, IGitHubGateway gitHub, string? returnUrl) =>
        Results.Redirect(gitHub.GetAuthorizationUrl(oauth.Begin(context, OAuthState.SanitizeReturnUrl(returnUrl))).ToString());

    private static async Task<IResult> CompleteGitHubSignInAsync(
        HttpContext context,
        OAuthState oauth,
        LoginWithGitHub login,
        SignInCodes codes,
        IOptions<FrontendOptions> frontend,
        string? code,
        string? state,
        CancellationToken ct)
    {
        var returnUrl = oauth.Complete(context, state);
        if (returnUrl is null || string.IsNullOrEmpty(code))
        {
            return Results.Redirect(frontend.Value.CallbackUrl("error=signin_failed"));
        }

        var user = await login.ExecuteAsync(code, ct);
        var query = $"code={codes.Issue(user.Id)}&returnUrl={Uri.EscapeDataString(returnUrl)}";
        return Results.Redirect(frontend.Value.CallbackUrl(query));
    }

    private static async Task<IResult> ExchangeSignInCodeAsync(
        ExchangeRequest body, SignInCodes codes, ProfileUseCases profiles, SessionTokenIssuer tokens, CancellationToken ct)
    {
        if (codes.Redeem(body.Code) is not { } userId)
        {
            return Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid sign-in code",
                detail: "The sign-in code is unknown, expired or already used. Sign in again.");
        }

        return await IssueTokenAsync(new CurrentUser(userId), profiles, tokens, ct);
    }

    private static async Task<IResult> IssueTokenAsync(CurrentUser me, ProfileUseCases profiles, SessionTokenIssuer tokens, CancellationToken ct)
    {
        var issued = tokens.Issue(await profiles.GetAsync(me.Id, ct));
        return Results.Ok(new TokenDto(issued.AccessToken, issued.ExpiresAt));
    }
}
