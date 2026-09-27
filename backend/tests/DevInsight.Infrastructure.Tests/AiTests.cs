using System.Text.Json;
using DevInsight.Application.Abstractions;
using DevInsight.Domain.Analyses;
using DevInsight.Domain.Analyses.Engine;
using DevInsight.Domain.Users;
using DevInsight.Infrastructure.Ai;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DevInsight.Infrastructure.Tests;

public class AiFindingsParserTests
{
    [Fact]
    public void Parses_findings_and_maps_enums()
    {
        const string json = """
            {"findings":[
              {"title":"Split Program.cs","message":"It holds 60% of the code.","severity":"high","category":"structure","isStrength":false},
              {"title":"Great tests","message":"25% test files.","severity":"low","category":"quality","isStrength":true}
            ]}
            """;

        var findings = AiFindingsParser.Parse(json, maxFindings: 5);

        findings.Count.ShouldBe(2);
        findings[0].ShouldBe(new FeedbackFinding("ai", MetricCategory.Structure, Severity.High, "Split Program.cs", "It holds 60% of the code."));
        findings[1].IsStrength.ShouldBeTrue();
    }

    [Fact]
    public void Drops_empty_findings_and_caps_the_count()
    {
        const string json = """{"findings":[{"title":"","message":"x"},{"title":"a","message":"b"},{"title":"c","message":"d"}]}""";
        AiFindingsParser.Parse(json, maxFindings: 1).ShouldHaveSingleItem().Title.ShouldBe("a");
    }

    [Fact]
    public void Unknown_enum_values_fall_back_safely() =>
        AiFindingsParser.Parse("""{"findings":[{"title":"t","message":"m","severity":"critical","category":"vibes"}]}""", 5)
            .Single().ShouldSatisfyAllConditions(f => f.Severity.ShouldBe(Severity.Low), f => f.Category.ShouldBe(MetricCategory.Quality));

    [Fact]
    public void Schema_forbids_extra_properties_and_bounds_the_count()
    {
        var schema = JsonSerializer.Serialize(AiFindingsParser.Schema(3));
        schema.ShouldContain("\"additionalProperties\":false");
        schema.ShouldContain("\"maxItems\":3");
        schema.ShouldContain("\"enum\":[\"low\",\"medium\",\"high\"]");
    }
}

public class ClaudeFeedbackGeneratorTests
{
    private static ClaudeFeedbackGenerator Generator(string? apiKey) =>
        new(Options.Create(new AiFeedbackOptions { ApiKey = apiKey }), NullLogger<ClaudeFeedbackGenerator>.Instance);

    [Fact]
    public async Task Disabled_without_an_api_key()
    {
        var generator = Generator(null);
        generator.IsEnabled.ShouldBeFalse();
        (await generator.GenerateAsync(Request(), TestContext.Current.CancellationToken)).ShouldBeEmpty();
    }

    [Fact]
    public void Request_is_grounded_structured_and_has_refusal_fallback()
    {
        var body = Generator("test-key").BuildRequest(Request()).ToString();

        body.ShouldContain("claude-opus-5");
        body.ShouldContain("json_schema");
        body.ShouldContain("server-side-fallback-2026-07-01");
        body.ShouldContain("\"fallbacks\": \"default\"");
        body.ShouldContain("octo/demo");
        body.ShouldContain(MetricKeys.VagueCommitRatio);
    }

    private static AiFeedbackRequest Request()
    {
        var snapshot = new RepositorySnapshot("sha", [new CommitRecord("s", "a", "a@x.io", DateTimeOffset.UtcNow, "fix", 1, 1, false, ["a.cs"])], [new SourceFile("a.cs", 10)]);
        var result = AnalysisEngine.Analyze(snapshot, AnalysisScope.Repo, new ContributorIdentity("a", ["a@x.io"]), DateTimeOffset.UtcNow);
        return new AiFeedbackRequest("octo/demo", "C#", Analysis.Create(Guid.NewGuid(), AnalysisScope.Repo, result, DateTimeOffset.UtcNow), []);
    }
}
