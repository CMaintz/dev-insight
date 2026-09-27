using DevInsight.Domain.Common;

namespace DevInsight.Domain.Analyses;

public enum FeedbackType
{
    RuleBased,
    Ai,
}

public enum Severity
{
    Low,
    Medium,
    High,
}

/// <summary>
/// One actionable observation about an analysis. <see cref="IsStrength"/> marks positive findings
/// ("good separation of concerns") so the UI can show what is going well, not only what is wrong.
/// </summary>
public sealed class Feedback
{
    private Feedback() { }

    public Guid Id { get; private set; }
    public Guid AnalysisId { get; private set; }
    public FeedbackType Type { get; private set; }
    public Severity Severity { get; private set; }
    public MetricCategory Category { get; private set; }

    /// <summary>The rule that produced this item (e.g. "vague-commits"), or "ai" for AI feedback.</summary>
    public string Source { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public bool IsStrength { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static Feedback Create(FeedbackType type, FeedbackFinding finding, DateTimeOffset now) => new()
    {
        Id = Ids.New(),
        Type = type,
        Severity = finding.Severity,
        Category = finding.Category,
        Source = finding.Source,
        Title = finding.Title,
        Message = finding.Message,
        IsStrength = finding.IsStrength,
        CreatedAt = now,
    };
}

public sealed record FeedbackFinding(
    string Source,
    MetricCategory Category,
    Severity Severity,
    string Title,
    string Message,
    bool IsStrength = false);
