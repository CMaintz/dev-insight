using DevInsight.Domain.Analyses;

namespace DevInsight.Api.Contracts;

// Wire contracts — documented in docs/API.md. Enums serialise as camelCase strings.

public sealed record ProfileDto(
    Guid Id, string Login, string? Name, string? Email, string? AvatarUrl,
    string? Bio, string? LinkedInUrl, bool IsPortfolioPublic, string PortfolioPath);

public sealed record UpdateProfileRequest(string? Bio, string? LinkedInUrl, bool IsPortfolioPublic);

public sealed record TokenDto(string AccessToken, DateTimeOffset ExpiresAt);

public sealed record RepositoryDto(
    Guid Id, string Name, string FullName, string? Description, string HtmlUrl, string? Language,
    int Stars, int Forks, bool IsFork, bool IsPrivate, bool IsArchived, DateTimeOffset? LastActivity,
    bool IsSelected, IReadOnlyDictionary<string, long> Languages);

public sealed record SelectRepositoryRequest(bool IsSelected);

public sealed record ImportSummaryDto(int Imported, int Updated, int Total);

public sealed record AnalysisRunDto(
    Guid Id, Guid RepositoryId, AnalysisScope Scope, AnalysisRunStatus Status, Guid? AnalysisId, string? Error,
    DateTimeOffset RequestedAt, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt);

public sealed record MetricDto(string Name, MetricCategory Category, double Value, bool IncludedInScore, int? Points, double? Weight);

public sealed record DimensionScoresDto(int Activity, int Structure, int Quality);

public sealed record ScoresDto(int Overall, int Activity, int Structure, int Quality);

public sealed record FeedbackDto(
    Guid Id, FeedbackType Type, Severity Severity, MetricCategory Category, string Source,
    string Title, string Message, bool IsStrength);

public sealed record AnalysisDto(
    Guid Id, Guid RepositoryId, AnalysisScope Scope, DateTimeOffset CreatedAt, string? HeadCommitSha,
    int OverallScore, DimensionScoresDto Scores, IReadOnlyList<MetricDto> Metrics,
    IReadOnlyList<ActivityWeek> Timeline, IReadOnlyList<FileSize> LargestFiles, IReadOnlyList<FeedbackDto> Feedback);

public sealed record ScoreSnapshotDto(Guid AnalysisId, DateTimeOffset CreatedAt, int Overall, int Activity, int Structure, int Quality);

public sealed record RepositorySummaryDto(RepositoryDto Repository, ScoresDto? Scores, DateTimeOffset? AnalyzedAt);

public sealed record FeedbackHighlightDto(Guid RepositoryId, string RepositoryName, FeedbackDto Feedback);

public sealed record CommitSizesDto(int Xs, int S, int M, int L, int Xl);

public sealed record CommitQualityDto(int Commits, int VagueCommits, CommitSizesDto Sizes);

public sealed record LanguageShareDto(string Language, long Bytes, double Share);

public sealed record ScorePointDto(DateOnly Date, int Overall, int Activity, int Structure, int Quality);

public sealed record DashboardDto(
    AnalysisScope Scope, int RepositoryCount, int SelectedCount, int AnalyzedCount, ScoresDto? AverageScores,
    IReadOnlyList<RepositorySummaryDto> Repositories, IReadOnlyList<LanguageShareDto> Languages,
    IReadOnlyList<ActivityWeek> Activity, IReadOnlyList<ScorePointDto> ScoreEvolution,
    CommitQualityDto CommitQuality, IReadOnlyList<FeedbackHighlightDto> TopFeedback);

public sealed record ProjectDto(
    Guid Id, string Name, string? Description, IReadOnlyList<string> ImageUrls,
    IReadOnlyList<Guid> LinkedRepositoryIds, int SortOrder);

public sealed record ProjectRequest(
    string Name, string? Description, IReadOnlyList<string>? ImageUrls,
    IReadOnlyList<Guid>? LinkedRepositoryIds, int SortOrder);

public sealed record PortfolioOwnerDto(
    string Login, string? Name, string? AvatarUrl, string? Bio, string? LinkedInUrl, string GitHubUrl, bool IsPublic);

public sealed record PortfolioRepositoryDto(
    Guid Id, string Name, string? Description, string HtmlUrl, string? Language, int Stars, int Forks,
    DateTimeOffset? LastActivity, IReadOnlyDictionary<string, long> Languages, ScoresDto? Scores);

public sealed record PortfolioProjectDto(
    Guid Id, string Name, string? Description, IReadOnlyList<string> ImageUrls, IReadOnlyList<Guid> LinkedRepositoryIds);

public sealed record StrengthDto(Guid RepositoryId, string RepositoryName, string Title, string Message);

public sealed record PortfolioDto(
    PortfolioOwnerDto Owner, AnalysisScope Scope, ScoresDto? Scores, IReadOnlyList<PortfolioRepositoryDto> Repositories,
    IReadOnlyList<PortfolioProjectDto> Projects, IReadOnlyList<LanguageShareDto> Languages,
    IReadOnlyList<ActivityWeek> Activity, IReadOnlyList<ScorePointDto> ScoreEvolution, IReadOnlyList<StrengthDto> Strengths);
