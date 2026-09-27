using DevInsight.Application.Abstractions;
using DevInsight.Application.Dashboard;
using DevInsight.Application.Portfolio;
using DevInsight.Domain.Analyses;
using DevInsight.Domain.Projects;
using DevInsight.Domain.Repositories;
using DevInsight.Domain.Users;

namespace DevInsight.Api.Contracts;

internal static class Mapping
{
    public static ProfileDto ToDto(this User u) =>
        new(u.Id, u.Login, u.Name, u.Email, u.AvatarUrl, u.Bio, u.LinkedInUrl, u.IsPortfolioPublic, $"/u/{u.Login}");

    public static RepositoryDto ToDto(this Repository r) =>
        new(r.Id, r.Name, r.FullName, r.Description, r.HtmlUrl, r.PrimaryLanguage, r.Stars, r.Forks,
            r.IsFork, r.IsPrivate, r.IsArchived, r.LastActivity, r.IsSelected, r.Languages);

    public static AnalysisRunDto ToDto(this AnalysisRun r) =>
        new(r.Id, r.RepositoryId, r.Scope, r.Status, r.AnalysisId, r.Error, r.RequestedAt, r.StartedAt, r.CompletedAt);

    public static FeedbackDto ToDto(this Feedback f) =>
        new(f.Id, f.Type, f.Severity, f.Category, f.Source, f.Title, f.Message, f.IsStrength);

    public static IReadOnlyList<FeedbackDto> ToDtos(this IEnumerable<Feedback> feedback) =>
        [.. feedback.OrderBy(f => f.IsStrength).ThenByDescending(f => f.Severity).Select(ToDto)];

    public static AnalysisDto ToDto(this Analysis a) =>
        new(a.Id, a.RepositoryId, a.Scope, a.CreatedAt, a.HeadCommitSha, a.OverallScore,
            new DimensionScoresDto(a.ActivityScore, a.StructureScore, a.QualityScore),
            [.. a.Metrics.Select(m => new MetricDto(m.Name, m.Category, m.Value, m.IncludedInScore, m.Points, m.Weight))],
            a.Timeline, a.LargestFiles, a.Feedback.ToDtos());

    public static ScoreSnapshotDto ToDto(this ScoreSnapshot s) =>
        new(s.AnalysisId, s.CreatedAt, s.OverallScore, s.ActivityScore, s.StructureScore, s.QualityScore);

    public static ScoresDto? ToScores(this Analysis? a) =>
        a is null ? null : new ScoresDto(a.OverallScore, a.ActivityScore, a.StructureScore, a.QualityScore);

    public static ScoresDto? ToDto(this Scores? s) =>
        s is null ? null : new ScoresDto(s.Overall, s.Activity, s.Structure, s.Quality);

    public static ProjectDto ToDto(this Project p) =>
        new(p.Id, p.Name, p.Description, p.ImageUrls, p.LinkedRepositoryIds, p.SortOrder);

    public static ProjectDetails ToDetails(this ProjectRequest r) =>
        new(r.Name, r.Description, r.ImageUrls ?? [], r.LinkedRepositoryIds ?? [], r.SortOrder);

    public static DashboardDto ToDto(this DashboardView d) =>
        new(d.Scope, d.RepositoryCount, d.SelectedCount, d.AnalyzedCount, d.AverageScores.ToDto(),
            [.. d.Repositories.Select(r => new RepositorySummaryDto(r.Repository.ToDto(), r.LatestAnalysis.ToScores(), r.LatestAnalysis?.CreatedAt))],
            d.Languages.ToDtos(), d.Activity, d.ScoreEvolution.ToDtos(), d.CommitQuality.ToDto(),
            [.. d.TopFeedback.Select(h => new FeedbackHighlightDto(h.RepositoryId, h.RepositoryName, h.Feedback.ToDto()))]);

    public static PortfolioDto ToDto(this PortfolioView p) =>
        new(
            new PortfolioOwnerDto(p.Owner.Login, p.Owner.Name, p.Owner.AvatarUrl, p.Owner.Bio, p.Owner.LinkedInUrl,
                $"https://github.com/{p.Owner.Login}", p.Owner.IsPortfolioPublic),
            GetPortfolio.PortfolioScope,
            p.Scores.ToDto(),
            [.. p.Repositories.Select(r => new PortfolioRepositoryDto(
                r.Repository.Id, r.Repository.Name, r.Repository.Description, r.Repository.HtmlUrl, r.Repository.PrimaryLanguage,
                r.Repository.Stars, r.Repository.Forks, r.Repository.LastActivity, r.Repository.Languages, r.LatestAnalysis.ToScores()))],
            [.. p.Projects.Select(x => new PortfolioProjectDto(x.Id, x.Name, x.Description, x.ImageUrls, x.LinkedRepositoryIds))],
            p.Languages.ToDtos(), p.Activity, p.ScoreEvolution.ToDtos(),
            [.. p.Strengths.Select(s => new StrengthDto(s.RepositoryId, s.RepositoryName, s.Feedback.Title, s.Feedback.Message))]);

    private static IReadOnlyList<LanguageShareDto> ToDtos(this IReadOnlyList<LanguageShare> languages) =>
        [.. languages.Select(l => new LanguageShareDto(l.Language, l.Bytes, l.Share))];

    private static IReadOnlyList<ScorePointDto> ToDtos(this IReadOnlyList<ScorePoint> points) =>
        [.. points.Select(p => new ScorePointDto(p.Date, p.Overall, p.Activity, p.Structure, p.Quality))];

    private static CommitQualityDto ToDto(this CommitQualitySummary c) =>
        new(c.Commits, c.VagueCommits, new CommitSizesDto(c.Sizes.Xs, c.Sizes.S, c.Sizes.M, c.Sizes.L, c.Sizes.Xl));
}
