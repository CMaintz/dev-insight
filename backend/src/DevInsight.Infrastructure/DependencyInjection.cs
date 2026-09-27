using DevInsight.Application.Abstractions;
using DevInsight.Infrastructure.Ai;
using DevInsight.Infrastructure.AnalysisRuns;
using DevInsight.Infrastructure.Git;
using DevInsight.Infrastructure.GitHub;
using DevInsight.Infrastructure.Persistence;
using DevInsight.Infrastructure.Persistence.Stores;
using DevInsight.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevInsight.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Resolved lazily so hosts (and integration tests) can supply the connection string late.
        services.AddDbContext<DevInsightDbContext>((provider, options) => options
            .UseNpgsql(provider.GetRequiredService<IConfiguration>().GetConnectionString("DevInsight") is { Length: > 0 } connectionString
                ? connectionString
                : throw new InvalidOperationException("Connection string 'DevInsight' is not configured."))
            .UseSnakeCaseNamingConvention());
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DevInsightDbContext>());

        services.AddScoped<IUserStore, EfUserStore>();
        services.AddScoped<IRepositoryStore, EfRepositoryStore>();
        services.AddScoped<IAnalysisStore, EfAnalysisStore>();
        services.AddScoped<IAnalysisRunStore, EfAnalysisRunStore>();
        services.AddScoped<IProjectStore, EfProjectStore>();
        services.AddScoped<IGitHubCredentialStore, ProtectedGitHubCredentialStore>();

        services.AddDataProtection()
            .SetApplicationName("DevInsight")
            .PersistKeysToDbContext<DevInsightDbContext>();

        services.Configure<GitHubOptions>(configuration.GetSection(GitHubOptions.Section));
        services.Configure<GitAnalysisOptions>(configuration.GetSection(GitAnalysisOptions.Section));
        services.Configure<AiFeedbackOptions>(configuration.GetSection(AiFeedbackOptions.Section));
        services.Configure<AnalysisWorkerOptions>(configuration.GetSection(AnalysisWorkerOptions.Section));

        services.AddSingleton<IGitHubGateway, OctokitGitHubGateway>();
        services.AddSingleton<IRepositorySnapshotSource, GitSnapshotSource>();
        services.AddSingleton<IAiFeedbackGenerator, ClaudeFeedbackGenerator>();
        services.AddSingleton<IAnalysisQueue, ChannelAnalysisQueue>();
        services.AddHostedService<AnalysisWorker>();
        services.AddHostedService<ScheduledReanalysisService>();
        return services;
    }
}
