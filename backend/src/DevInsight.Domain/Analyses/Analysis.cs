using DevInsight.Domain.Analyses.Engine;
using DevInsight.Domain.Common;

namespace DevInsight.Domain.Analyses;

/// <summary>
/// A point-in-time analysis of one repository in one scope. Analyses are append-only snapshots:
/// the sequence of analyses for a repository is its score history.
/// </summary>
public sealed class Analysis
{
    private readonly List<AnalysisMetric> _metrics = [];
    private readonly List<Feedback> _feedback = [];

    private Analysis() { }

    public Guid Id { get; private set; }
    public Guid RepositoryId { get; private set; }
    public AnalysisScope Scope { get; private set; }
    public int OverallScore { get; private set; }
    public int ActivityScore { get; private set; }
    public int StructureScore { get; private set; }
    public int QualityScore { get; private set; }
    public string? HeadCommitSha { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyList<AnalysisMetric> Metrics => _metrics;
    public IReadOnlyList<Feedback> Feedback => _feedback;
    public List<ActivityWeek> Timeline { get; private set; } = [];
    public List<FileSize> LargestFiles { get; private set; } = [];

    public static Analysis Create(Guid repositoryId, AnalysisScope scope, AnalysisResult result, DateTimeOffset now)
    {
        var analysis = new Analysis
        {
            Id = Ids.New(),
            RepositoryId = repositoryId,
            Scope = scope,
            ActivityScore = result.ActivityScore,
            StructureScore = result.StructureScore,
            QualityScore = result.QualityScore,
            OverallScore = Scoring.Overall(result.ActivityScore, result.StructureScore, result.QualityScore),
            HeadCommitSha = result.HeadCommitSha,
            Timeline = [.. result.Timeline],
            LargestFiles = [.. result.LargestFiles],
            CreatedAt = now,
        };
        analysis._metrics.AddRange(result.Metrics);
        return analysis;
    }

    public double? MetricValue(string name) => _metrics.FirstOrDefault(m => m.Name == name)?.Value;

    public void AddFeedback(IEnumerable<Feedback> feedback) => _feedback.AddRange(feedback);
}
