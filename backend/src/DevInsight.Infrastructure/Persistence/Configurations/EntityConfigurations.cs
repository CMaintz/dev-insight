using DevInsight.Domain.Analyses;
using DevInsight.Domain.Projects;
using DevInsight.Domain.Repositories;
using DevInsight.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevInsight.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.GitHubId).HasColumnName("github_id");
        builder.HasIndex(u => u.GitHubId).IsUnique();
        // Not unique: GitHub logins can be renamed and later reclaimed by another account.
        builder.HasIndex(u => u.Login);
        builder.Property(u => u.Login).HasMaxLength(100);
        builder.Property(u => u.Name).HasMaxLength(255);
        builder.Property(u => u.Email).HasMaxLength(320);
        builder.Property(u => u.AvatarUrl).HasMaxLength(2048);
        builder.Property(u => u.Bio).HasMaxLength(2000);
        builder.Property(u => u.LinkedInUrl).HasMaxLength(2048);
    }
}

internal sealed class RepositoryConfiguration : IEntityTypeConfiguration<Repository>
{
    public void Configure(EntityTypeBuilder<Repository> builder)
    {
        builder.HasKey(r => r.Id);
        // One row per (user, GitHub repository): the guard against duplicate imports.
        builder.Property(r => r.GitHubRepoId).HasColumnName("github_repo_id");
        builder.HasIndex(r => new { r.UserId, r.GitHubRepoId }).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Ignore(r => r.FullName);
        builder.Property(r => r.Owner).HasMaxLength(100);
        builder.Property(r => r.Name).HasMaxLength(100);
        builder.Property(r => r.HtmlUrl).HasMaxLength(2048);
        builder.Property(r => r.PrimaryLanguage).HasMaxLength(100);
        builder.Property(r => r.DefaultBranch).HasMaxLength(255);
        builder.Property(r => r.Languages).HasJsonConversion();
    }
}

internal sealed class AnalysisConfiguration : IEntityTypeConfiguration<Analysis>
{
    public void Configure(EntityTypeBuilder<Analysis> builder)
    {
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => new { a.RepositoryId, a.Scope, a.CreatedAt });
        builder.HasOne<Repository>().WithMany().HasForeignKey(a => a.RepositoryId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(a => a.HeadCommitSha).HasMaxLength(64);
        builder.Property(a => a.Timeline).HasJsonConversion();
        builder.Property(a => a.LargestFiles).HasJsonConversion();
        builder.HasMany(a => a.Metrics).WithOne().HasForeignKey(m => m.AnalysisId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(a => a.Feedback).WithOne().HasForeignKey(f => f.AnalysisId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(a => a.Metrics).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(a => a.Feedback).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class AnalysisMetricConfiguration : IEntityTypeConfiguration<AnalysisMetric>
{
    public void Configure(EntityTypeBuilder<AnalysisMetric> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Name).HasMaxLength(64);
        builder.HasIndex(m => new { m.AnalysisId, m.Name }).IsUnique();
    }
}

internal sealed class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Source).HasMaxLength(64);
        builder.Property(f => f.Title).HasMaxLength(200);
        builder.Property(f => f.Message).HasMaxLength(4000);
    }
}

internal sealed class AnalysisRunConfiguration : IEntityTypeConfiguration<AnalysisRun>
{
    public void Configure(EntityTypeBuilder<AnalysisRun> builder)
    {
        builder.HasKey(r => r.Id);
        builder.HasIndex(r => new { r.RepositoryId, r.Scope, r.Status });
        builder.HasIndex(r => new { r.UserId, r.RequestedAt });
        builder.HasOne<Repository>().WithMany().HasForeignKey(r => r.RepositoryId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(r => r.Error).HasMaxLength(1000);
        builder.Ignore(r => r.IsFinished);
    }
}

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.HasKey(p => p.Id);
        builder.HasIndex(p => new { p.UserId, p.SortOrder });
        builder.HasOne<User>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(p => p.Name).HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(4000);
    }
}

internal sealed class GitHubCredentialConfiguration : IEntityTypeConfiguration<GitHubCredential>
{
    public void Configure(EntityTypeBuilder<GitHubCredential> builder)
    {
        builder.ToTable("github_credentials");
        builder.HasKey(c => c.UserId);
        builder.HasOne<User>().WithOne().HasForeignKey<GitHubCredential>(c => c.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
