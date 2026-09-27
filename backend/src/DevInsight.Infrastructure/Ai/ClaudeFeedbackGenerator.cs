using System.Text.Json;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;
using DevInsight.Application.Abstractions;
using DevInsight.Domain.Analyses;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevInsight.Infrastructure.Ai;

public sealed class AiFeedbackOptions
{
    public const string Section = "AiFeedback";

    /// <summary>Anthropic API key. AI feedback is disabled when empty.</summary>
    public string? ApiKey { get; set; }

    public string Model { get; set; } = "claude-opus-5";

    public int MaxFindings { get; set; } = 5;
}

/// <summary>
/// AI feedback grounded in the analysis: Claude sees only the computed metrics, scores, largest files
/// and the rule-based findings, and must return findings that cite that data. Structured output
/// guarantees the response parses into <see cref="FeedbackFinding"/>s.
/// </summary>
internal sealed class ClaudeFeedbackGenerator(IOptions<AiFeedbackOptions> options, ILogger<ClaudeFeedbackGenerator> logger)
    : IAiFeedbackGenerator
{
    private const string SystemPrompt = """
        You review software repositories for a developer-insight platform. You receive measured metrics
        for one repository (or for one developer's contributions to it), its scores, its largest files and
        the findings a rule engine already produced.

        Write concrete, actionable feedback that a developer can act on this week:
        - Ground every finding in the supplied data: cite the metric values, file paths or scores it rests on.
        - Do not repeat the rule-based findings; add what they miss (patterns across metrics, priorities, next steps).
        - Include at least one strength when the data supports it (isStrength = true).
        - Never invent facts that are not in the data (no guesses about code you have not seen).
        - Keep each message under 60 words, direct and specific.
        """;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly AiFeedbackOptions _options = options.Value;
    private readonly AnthropicClient? _client = string.IsNullOrWhiteSpace(options.Value.ApiKey)
        ? null
        : new AnthropicClient { ApiKey = options.Value.ApiKey };

    public bool IsEnabled => _client is not null;

    public async Task<IReadOnlyList<FeedbackFinding>> GenerateAsync(AiFeedbackRequest request, CancellationToken cancellationToken)
    {
        if (_client is null)
        {
            return [];
        }

        try
        {
            var response = await _client.Beta.Messages.Create(BuildRequest(request), cancellationToken);
            if (response.StopReason == "refusal")
            {
                logger.LogWarning("AI feedback was declined for {Repository}", request.RepositoryName);
                return [];
            }

            var text = string.Concat(response.Content.Select(b => b.Value).OfType<BetaTextBlock>().Select(t => t.Text));
            return AiFindingsParser.Parse(text, _options.MaxFindings);
        }
        catch (AnthropicRateLimitException ex)
        {
            logger.LogWarning(ex, "AI feedback rate-limited for {Repository}", request.RepositoryName);
            return [];
        }
        catch (AnthropicApiException ex)
        {
            logger.LogWarning(ex, "AI feedback request failed for {Repository}", request.RepositoryName);
            return [];
        }
    }

    internal MessageCreateParams BuildRequest(AiFeedbackRequest request) => new()
    {
        Model = _options.Model,
        MaxTokens = 16000,
        // If the primary model declines, the API re-serves the request with a fallback model it picks
        // for the refusal category ("default" routing), inside the same call.
        Betas = ["server-side-fallback-2026-07-01"],
        Fallbacks = new BetaFallbacksParam(new Default()),
        System = SystemPrompt,
        OutputConfig = new BetaOutputConfig
        {
            Format = new BetaJsonOutputFormat { Schema = AiFindingsParser.Schema(_options.MaxFindings) },
        },
        Messages =
        [
            new() { Role = Role.User, Content = JsonSerializer.Serialize(ToPromptData(request), Json) },
        ],
    };

    private static object ToPromptData(AiFeedbackRequest request)
    {
        var analysis = request.Analysis;
        return new
        {
            repository = request.RepositoryName,
            primaryLanguage = request.PrimaryLanguage,
            scope = analysis.Scope == AnalysisScope.Repo ? "whole repository" : "only this developer's commits and the files they touched",
            scores = new { analysis.OverallScore, analysis.ActivityScore, analysis.StructureScore, analysis.QualityScore },
            metrics = analysis.Metrics.Select(m => new { m.Name, category = m.Category.ToString(), m.Value, m.Points, m.Weight }),
            largestFiles = analysis.LargestFiles,
            ruleFindings = request.RuleFeedback.Select(f => new { f.Title, severity = f.Severity.ToString(), f.IsStrength }),
        };
    }
}
