using DevInsight.Domain.Analyses;
using DevInsight.Domain.Analyses.Engine;
using DevInsight.Domain.Analyses.Rules;
using static DevInsight.Domain.Tests.TestData;

namespace DevInsight.Domain.Tests;

public class FeedbackRuleTests
{
    private static IReadOnlyList<Feedback> Evaluate(
        IReadOnlyList<CommitRecord> commits,
        IReadOnlyList<SourceFile>? files = null,
        AnalysisScope scope = AnalysisScope.Repo)
    {
        var result = AnalysisEngine.Analyze(new RepositorySnapshot("sha", commits, files ?? HealthyFiles()), scope, Identity, Now);
        return FeedbackRuleSet.Default.Evaluate(Analysis.Create(Guid.NewGuid(), scope, result, Now), Now);
    }

    [Fact]
    public void Healthy_project_gets_strength_findings()
    {
        var feedback = Evaluate(WeeklyCommits(20));

        feedback.Count.ShouldBeGreaterThanOrEqualTo(2);
        feedback.ShouldContain(f => f.Source == "descriptive-commits" && f.IsStrength);
        feedback.ShouldContain(f => f.Source == "separation-of-concerns" && f.IsStrength);
        feedback.ShouldAllBe(f => f.Type == FeedbackType.RuleBased);
    }

    [Fact]
    public void Commit_message_feedback_is_always_given() =>
        Evaluate([Commit(1, "fix"), Commit(2, "update")], [File("src/Program.cs", 3000)])
            .ShouldContain(f => f.Category == MetricCategory.CommitQuality);

    [Fact]
    public void Structure_feedback_is_always_given() =>
        Evaluate([Commit(1, "fix"), Commit(2, "update")], [File("src/Program.cs", 3000)])
            .ShouldContain(f => f.Category == MetricCategory.Structure);

    [Fact]
    public void Mostly_vague_commits_is_high_severity()
    {
        var feedback = Evaluate([Commit(1, "fix"), Commit(2, "stuff"), Commit(3, "Add import endpoint for repos")]);
        var vague = feedback.Single(f => f.Source == "vague-commits");
        vague.Severity.ShouldBe(Severity.High);
        vague.Message.ShouldContain("2 commits (67%)");
    }

    [Fact]
    public void Monolith_names_the_largest_file()
    {
        var feedback = Evaluate([Commit(1)], [File("src/Program.cs", 3000), File("src/Util.cs", 100)]);
        var monolith = feedback.Single(f => f.Source == "monolith");
        monolith.Severity.ShouldBe(Severity.High);
        monolith.Message.ShouldContain("src/Program.cs (3000 lines)");
    }

    [Fact]
    public void Missing_project_hygiene_is_reported()
    {
        var feedback = Evaluate([Commit(1)], [File("main.py", 100)]);
        feedback.Select(f => f.Source).ShouldBe(
            ["missing-tests", "missing-readme", "missing-ci", "missing-lint", "descriptive-commits", "separation-of-concerns"],
            ignoreOrder: true);
    }

    [Fact]
    public void Dormant_project_is_flagged()
    {
        Evaluate([Commit(daysAgo: 400)]).ShouldContain(f => f.Source == "dormant");
    }

    [Fact]
    public void User_without_attributed_commits_is_told_why()
    {
        var feedback = Evaluate(WeeklyCommits(3, OtherEmail), scope: AnalysisScope.UserContribution);
        feedback.Single(f => f.Source == "no-commits").Message.ShouldContain("verified email");
    }

    [Fact]
    public void Findings_are_ordered_most_severe_first()
    {
        var severities = Evaluate([Commit(1, "fix")], [File("main.py", 100)]).Select(f => f.Severity).ToList();
        severities.ShouldBe(severities.OrderByDescending(s => s).ToList());
    }
}
