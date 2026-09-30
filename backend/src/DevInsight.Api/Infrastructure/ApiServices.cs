using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using DevInsight.Api.Auth;
using DevInsight.Api.Endpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DevInsight.Api.Infrastructure;

internal static class ApiServices
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
        services.AddProblemDetails();
        services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
        services.AddOpenApi();
        services.AddHealthChecks();

        // Behind a TLS-terminating proxy (container platform), trust X-Forwarded-Proto so cookies get Secure.
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        AddAuth(services, configuration);
        AddRateLimits(services);
        return services;
    }

    private static void AddAuth(IServiceCollection services, IConfiguration configuration)
    {
        AddSignIn(services, configuration);
        AddCrossOriginSpa(services, configuration);
        AddJwtBearer(services, configuration);
        services.AddAuthorization();
    }

    private static void AddSignIn(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));
        services.AddSingleton<SessionTokenIssuer>();
        services.AddSingleton<OAuthState>();
        services.AddMemoryCache();
        services.AddSingleton<SignInCodes>();
    }

    /// <summary>The SPA may live on another origin (GitHub Pages). Bearer tokens, no cookies ⇒ no credentials mode.</summary>
    private static void AddCrossOriginSpa(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FrontendOptions>(configuration.GetSection(FrontendOptions.Section));
        services.AddCors();
        services.AddOptions<CorsOptions>().Configure<IOptions<FrontendOptions>>((cors, frontend) =>
        {
            if (frontend.Value.Origin is { } origin)
            {
                cors.AddDefaultPolicy(policy => policy
                    .WithOrigins(origin)
                    .WithHeaders("Authorization", "Content-Type")
                    .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
                    .WithExposedHeaders("Location"));
            }
        });
    }

    private static void AddJwtBearer(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((options, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = jwt.GetSecurityKey(),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });
    }

    private static void AddRateLimits(IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(ApiEndpoints.AnalysisRateLimit, context => PerUser(context, permitsPerMinute: 30));
            options.AddPolicy(ApiEndpoints.ImportRateLimit, context => PerUser(context, permitsPerMinute: 5));
            options.AddPolicy(AuthEndpoints.ExchangeRateLimit, context => PerUser(context, permitsPerMinute: 20));
        });

    private static RateLimitPartition<string> PerUser(HttpContext context, int permitsPerMinute) =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.User.FindUserId()?.ToString() ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permitsPerMinute, Window = TimeSpan.FromMinutes(1) });
}

internal static class SecurityHeaders
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; "
        + "font-src 'self'; img-src 'self' https: data:; connect-src 'self'; "
        + "frame-ancestors 'none'; base-uri 'self'; form-action 'self'";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            // The Scalar API reference loads its UI from a CDN; everything else is same-origin only.
            if (!context.Request.Path.StartsWithSegments("/scalar"))
            {
                headers.ContentSecurityPolicy = ContentSecurityPolicy;
            }

            await next();
        });
}
