using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DevInsight.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DevInsight.Api.Auth;

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    private const int MinKeyBytes = 32;

    public string Issuer { get; set; } = "devinsight";
    public string Audience { get; set; } = "devinsight";
    public string SigningKey { get; set; } = string.Empty;
    public int LifetimeDays { get; set; } = 7;

    public SymmetricSecurityKey GetSecurityKey()
    {
        var bytes = Encoding.UTF8.GetBytes(SigningKey);
        return bytes.Length >= MinKeyBytes
            ? new SymmetricSecurityKey(bytes)
            : throw new InvalidOperationException($"Jwt:SigningKey must be at least {MinKeyBytes} bytes.");
    }
}

public sealed record IssuedToken(string AccessToken, DateTimeOffset ExpiresAt);

/// <summary>Issues the signed JWT that represents a DevInsight session.</summary>
public sealed class SessionTokenIssuer(IOptions<JwtOptions> options, TimeProvider clock)
{
    public IssuedToken Issue(User user)
    {
        var jwt = options.Value;
        var now = clock.GetUtcNow();
        var expires = now.AddDays(jwt.LifetimeDays);
        var token = new JwtSecurityToken(
            jwt.Issuer,
            jwt.Audience,
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Login),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            ],
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(jwt.GetSecurityKey(), SecurityAlgorithms.HmacSha256));
        return new IssuedToken(new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}

/// <summary>The session cookie: HttpOnly so scripts never see the token; SameSite=Strict so it is never sent cross-site.</summary>
public static class SessionCookie
{
    public const string Name = "devinsight_session";

    public static void Append(HttpContext context, IssuedToken token) =>
        context.Response.Cookies.Append(Name, token.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Strict,
            Expires = token.ExpiresAt,
            Path = "/",
        });

    public static void Delete(HttpContext context) =>
        context.Response.Cookies.Delete(Name, new CookieOptions { Path = "/", SameSite = SameSiteMode.Strict });
}

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : throw new InvalidOperationException("The authenticated principal has no user id.");

    public static Guid? FindUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;
}
