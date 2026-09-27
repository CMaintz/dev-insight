using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace DevInsight.Api.Auth;

/// <summary>
/// CSRF protection for the OAuth round trip: a random state value is sent to GitHub and also kept,
/// encrypted, in a short-lived cookie together with the return URL. The callback must present the
/// same state. SameSite=Lax because the callback is a cross-site top-level navigation from GitHub.
/// </summary>
public sealed class OAuthState(IDataProtectionProvider protection, TimeProvider clock)
{
    private const string CookieName = "devinsight_oauth";
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private readonly ITimeLimitedDataProtector _protector =
        protection.CreateProtector("DevInsight.OAuthState.v1").ToTimeLimitedDataProtector();

    public string Begin(HttpContext context, string returnUrl)
    {
        var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var payload = JsonSerializer.Serialize(new StatePayload(state, returnUrl));
        context.Response.Cookies.Append(CookieName, _protector.Protect(payload, clock.GetUtcNow().Add(Lifetime)), new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = Lifetime,
            Path = "/api/auth",
        });
        return state;
    }

    /// <summary>The return URL if <paramref name="state"/> matches the cookie; otherwise null.</summary>
    public string? Complete(HttpContext context, string? state)
    {
        var cookie = context.Request.Cookies[CookieName];
        context.Response.Cookies.Delete(CookieName, new CookieOptions { Path = "/api/auth" });
        if (string.IsNullOrEmpty(cookie) || string.IsNullOrEmpty(state))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<StatePayload>(_protector.Unprotect(cookie));
            var matches = payload is not null && CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(payload.State), System.Text.Encoding.UTF8.GetBytes(state));
            return matches ? payload!.ReturnUrl : null;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    /// <summary>Only local paths are allowed as return URLs — never another origin (open-redirect guard).</summary>
    public static string SanitizeReturnUrl(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//", StringComparison.Ordinal)
            && !returnUrl.StartsWith("/\\", StringComparison.Ordinal)
            ? returnUrl
            : "/dashboard";

    private sealed record StatePayload(string State, string ReturnUrl);
}
