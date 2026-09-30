namespace DevInsight.Domain.Analyses.Rules;

/// <summary>
/// The rule-based feedback engine. With any commits and code in scope it always yields at least two
/// items: one about commit messages and one about structure.
/// </summary>
public sealed class FeedbackRuleSet(IEnumerable<IFeedbackRule> rules)
{
    public static FeedbackRuleSet Default { get; } = new(
    [
        new ActivityRule(),
        new CommitMessageRule(),
        new LargeCommitsRule(),
        new StructureRule(),
        new TestsRule(),
        new ProjectHygieneRule(),
    ]);

    public IReadOnlyList<Feedback> Evaluate(Analysis analysis, DateTimeOffset now) =>
    [
        .. rules
            .SelectMany(rule => rule.Evaluate(analysis))
            .OrderByDescending(f => f.Severity)
            .ThenBy(f => f.IsStrength)
            .Select(finding => Feedback.Create(FeedbackType.RuleBased, finding, now)),
    ];
}
