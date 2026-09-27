using DevInsight.Api.Auth;
using DevInsight.Api.Contracts;
using DevInsight.Application.Abstractions;
using DevInsight.Application.Auth;
using DevInsight.Application.Profile;

namespace DevInsight.Api.Endpoints;

internal static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        auth.MapGet("/github/login", (HttpContext context, OAuthState oauth, IGitHubGateway gitHub, string? returnUrl) =>
            Results.Redirect(gitHub.GetAuthorizationUrl(oauth.Begin(context, OAuthState.SanitizeReturnUrl(returnUrl))).ToString()))
            .WithSummary("Start GitHub sign-in (redirects to GitHub).");

        auth.MapGet("/github/callback", async (
            HttpContext context,
            OAuthState oauth,
            LoginWithGitHub login,
            SessionTokenIssuer tokens,
            string? code,
            string? state,
            CancellationToken ct) =>
        {
            var returnUrl = oauth.Complete(context, state);
            if (returnUrl is null || string.IsNullOrEmpty(code))
            {
                return Results.Redirect("/?signin=failed");
            }

            var user = await login.ExecuteAsync(code, ct);
            SessionCookie.Append(context, tokens.Issue(user));
            return Results.Redirect(returnUrl);
        })
        .WithSummary("GitHub OAuth callback: creates or updates the user and starts a session.");

        auth.MapPost("/logout", (HttpContext context) =>
        {
            SessionCookie.Delete(context);
            return Results.NoContent();
        })
        .WithSummary("End the session.");

        auth.MapPost("/token", async (HttpContext context, ProfileUseCases profiles, SessionTokenIssuer tokens, CancellationToken ct) =>
        {
            var issued = tokens.Issue(await profiles.GetAsync(context.User.GetUserId(), ct));
            return Results.Ok(new TokenDto(issued.AccessToken, issued.ExpiresAt));
        })
        .RequireAuthorization()
        .WithSummary("Issue a bearer JWT for API clients.");
    }
}
