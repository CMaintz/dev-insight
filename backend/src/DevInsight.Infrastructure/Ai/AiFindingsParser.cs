using System.Text.Json;
using DevInsight.Domain.Analyses;

namespace DevInsight.Infrastructure.Ai;

/// <summary>The JSON contract between the AI model and the domain: schema out, findings in.</summary>
internal static class AiFindingsParser
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static Dictionary<string, JsonElement> Schema(int maxFindings)
    {
        var finding = new
        {
            type = "object",
            additionalProperties = false,
            required = new[] { "title", "message", "severity", "category", "isStrength" },
            properties = new
            {
                title = new { type = "string" },
                message = new { type = "string" },
                severity = new { type = "string", @enum = new[] { "low", "medium", "high" } },
                category = new { type = "string", @enum = new[] { "activity", "commitQuality", "structure", "quality" } },
                isStrength = new { type = "boolean" },
            },
        };
        return new Dictionary<string, JsonElement>
        {
            ["type"] = JsonSerializer.SerializeToElement("object"),
            ["additionalProperties"] = JsonSerializer.SerializeToElement(false),
            ["required"] = JsonSerializer.SerializeToElement(new[] { "findings" }),
            ["properties"] = JsonSerializer.SerializeToElement(new
            {
                findings = new { type = "array", maxItems = maxFindings, items = finding },
            }),
        };
    }

    public static IReadOnlyList<FeedbackFinding> Parse(string json, int maxFindings)
    {
        var response = JsonSerializer.Deserialize<AiResponse>(json, Json);
        return
        [
            .. (response?.Findings ?? [])
                .Where(f => !string.IsNullOrWhiteSpace(f.Title) && !string.IsNullOrWhiteSpace(f.Message))
                .Take(maxFindings)
                .Select(f => new FeedbackFinding(
                    "ai",
                    ParseEnum(f.Category, MetricCategory.Quality),
                    ParseEnum(f.Severity, Severity.Low),
                    Truncate(f.Title!, 200),
                    Truncate(f.Message!, 4000),
                    f.IsStrength)),
        ];
    }

    private static T ParseEnum<T>(string? value, T fallback)
        where T : struct, Enum =>
        Enum.TryParse<T>(value, ignoreCase: true, out var parsed) ? parsed : fallback;

    private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max];

    private sealed record AiResponse(List<AiFinding>? Findings);

    private sealed record AiFinding(string? Title, string? Message, string? Severity, string? Category, bool IsStrength);
}
