using DevInsight.Domain.Analyses.Engine;
using static DevInsight.Domain.Analyses.Rules.AnalysisMetricExtensions;

namespace DevInsight.Domain.Analyses.Rules;

/// <summary>Always yields exactly one commit-message finding when there are commits: a problem or a strength.</summary>
public sealed class CommitMessageRule : IFeedbackRule
{
    public IEnumerable<FeedbackFinding> Evaluate(Analysis analysis)
    {
        if (!analysis.HasCommits())
        {
            yield break;
        }

        var ratio = analysis.Metric(MetricKeys.VagueCommitRatio);
        var count = (int)analysis.Metric(MetricKeys.VagueCommitCount);
        const string advice = "Describe intent and impact, e.g. \"fix: prevent duplicate repository import on re-login\" instead of \"fix\".";
        if (ratio >= 0.3)
        {
            yield return new FeedbackFinding("vague-commits", MetricCategory.CommitQuality, Severity.High,
                "Commit messages are often vague", $"{count} commits ({Percent(ratio)}) have messages like \"fix\" or \"update\". {advice}");
        }
        else if (ratio >= 0.1)
        {
            yield return new FeedbackFinding("vague-commits", MetricCategory.CommitQuality, Severity.Medium,
                "Some commit messages are vague", $"{count} commits ({Percent(ratio)}) have vague messages. {advice}");
        }
        else
        {
            yield return new FeedbackFinding("descriptive-commits", MetricCategory.CommitQuality, Severity.Low,
                "Commit messages are descriptive",
                "Almost every commit explains what it changes — history is easy to read and to bisect.", IsStrength: true);
        }
    }
}

public sealed class LargeCommitsRule : IFeedbackRule
{
    public IEnumerable<FeedbackFinding> Evaluate(Analysis analysis)
    {
        var ratio = analysis.Metric(MetricKeys.LargeCommitRatio);
        if (ratio >= 0.2)
        {
            yield return new FeedbackFinding("large-commits", MetricCategory.CommitQuality, Severity.Medium,
                "Many commits are very large",
                $"{Percent(ratio)} of commits change {CommitQualityAnalyzer.LargeCommitLines}+ lines. "
                + "Smaller, focused commits are easier to review and to revert.");
        }
    }
}

public sealed class ActivityRule : IFeedbackRule
{
    public IEnumerable<FeedbackFinding> Evaluate(Analysis analysis)
    {
        if (!analysis.HasCommits())
        {
            yield return new FeedbackFinding("no-commits", MetricCategory.Activity, Severity.High,
                "No commits found in this scope",
                analysis.Scope == AnalysisScope.UserContribution
                    ? "None of the commits are attributed to you. If you committed with another email, add it to your GitHub account as a verified email."
                    : "The repository has no commit history on its default branch.");
            yield break;
        }

        var days = analysis.Metric(MetricKeys.DaysSinceLastCommit);
        if (analysis.Metric(MetricKeys.IsDormant) > 0)
        {
            yield return new FeedbackFinding("dormant", MetricCategory.Activity, Severity.Medium,
                "Project looks dormant",
                $"The last commit was {days:0} days ago. Archive it, or note its status in the README so visitors know it is finished rather than abandoned.");
        }
        else if (analysis.Metric(MetricKeys.ActiveWeeksRatio) >= 0.5)
        {
            yield return new FeedbackFinding("consistent-activity", MetricCategory.Activity, Severity.Low,
                "Consistent activity",
                $"Commits landed in {Percent(analysis.Metric(MetricKeys.ActiveWeeksRatio))} of the last 26 weeks.", IsStrength: true);
        }
    }
}
