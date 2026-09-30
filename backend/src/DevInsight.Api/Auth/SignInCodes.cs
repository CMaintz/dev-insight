using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace DevInsight.Api.Auth;

/// <summary>
/// Hands a session from the OAuth callback to the SPA without putting a token in a URL: the callback
/// redirects with a random single-use code (60 s), which the SPA exchanges for a JWT via
/// <c>POST /api/auth/exchange</c> — the same shape as an OAuth authorization code.
/// </summary>
/// <remarks>Codes live in process memory, so a sign-in must be completed on the replica that started it
/// (true for a single replica; use a distributed cache before scaling out).</remarks>
public sealed class SignInCodes(IMemoryCache cache)
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(60);
    private readonly Lock _redeemLock = new();

    public string Issue(Guid userId)
    {
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        cache.Set(Key(code), userId, Lifetime);
        return code;
    }

    /// <summary>The user the code was issued for, or null if it is unknown, expired or already used.</summary>
    public Guid? Redeem(string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return null;
        }

        // Check-and-remove must be atomic, or two concurrent exchanges could both redeem one code.
        lock (_redeemLock)
        {
            if (!cache.TryGetValue(Key(code), out Guid userId))
            {
                return null;
            }

            cache.Remove(Key(code));
            return userId;
        }
    }

    private static string Key(string code) => $"signin-code:{code}";
}

public sealed class FrontendOptions
{
    public const string Section = "Frontend";

    /// <summary>
    /// Absolute URL of the SPA when it is hosted on another origin (e.g. https://cmaintz.github.io/dev-insight).
    /// Empty when the API serves the SPA itself.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    public string CallbackUrl(string query) => $"{Url.TrimEnd('/')}/auth/callback?{query}";

    public string? Origin => Uri.TryCreate(Url, UriKind.Absolute, out var uri) ? uri.GetLeftPart(UriPartial.Authority) : null;
}
