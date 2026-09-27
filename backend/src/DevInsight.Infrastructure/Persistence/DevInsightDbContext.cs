using DevInsight.Application.Abstractions;
using DevInsight.Domain.Analyses;
using DevInsight.Domain.Projects;
using DevInsight.Domain.Repositories;
using DevInsight.Domain.Users;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DevInsight.Infrastructure.Persistence;

public sealed class DevInsightDbContext(DbContextOptions<DevInsightDbContext> options)
    : DbContext(options), IUnitOfWork, IDataProtectionKeyContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Repository> Repositories => Set<Repository>();
    public DbSet<Analysis> Analyses => Set<Analysis>();
    public DbSet<AnalysisMetric> AnalysisMetrics => Set<AnalysisMetric>();
    public DbSet<Feedback> Feedback => Set<Feedback>();
    public DbSet<AnalysisRun> AnalysisRuns => Set<AnalysisRun>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<GitHubCredential> GitHubCredentials => Set<GitHubCredential>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) => SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DevInsightDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<AnalysisScope>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<AnalysisRunStatus>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<MetricCategory>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<FeedbackType>().HaveConversion<string>().HaveMaxLength(32);
        configurationBuilder.Properties<Severity>().HaveConversion<string>().HaveMaxLength(16);
    }
}

/// <summary>A user's GitHub OAuth token, encrypted with ASP.NET Core Data Protection.</summary>
public sealed class GitHubCredential
{
    public Guid UserId { get; set; }
    public string ProtectedToken { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}
