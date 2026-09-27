using System.Text.RegularExpressions;

namespace DevInsight.Domain.Analyses.Engine;

/// <summary>Rates commit message subjects. Vague messages ("fix", "stuff", "update") score zero.</summary>
public static partial class CommitMessageClassifier
{
    private const int MinDescriptiveLength = 8;
    private const int IdealMinLength = 15;
    private const int IdealMaxLength = 72;

    private static readonly HashSet<string> VagueMessages = new(StringComparer.OrdinalIgnoreCase)
    {
        "fix", "fixes", "fixed", "fixing", "fix bug", "bug fix", "bugfix", "fixed bug", "fixed stuff",
        "fix stuff", "stuff", "update", "updates", "updated", "changes", "change", "changed", "wip",
        "misc", "minor", "minor changes", "minor fix", "minor fixes", "tweak", "tweaks", "asdf", "test",
        "testing", "commit", "more", "done", "save", "temp", "tmp", "cleanup", "clean up", "refactor",
        "stuff and things", "small fix", "small fixes", "work", "progress", "edit", "edits", "oops",
        "...", ".", "-", "x", "a", "aa", "aaa", "idk", "things", "final", "final fix", "new", "add",
    };

    public static bool IsVague(string subject)
    {
        var normalized = Normalize(subject);
        if (normalized.Length < MinDescriptiveLength || VagueMessages.Contains(normalized))
        {
            return true;
        }

        // A conventional-commit prefix around a vague body ("fix: stuff") is still vague.
        var body = ConventionalPrefix().Replace(normalized, string.Empty).Trim();
        return body.Length < 4 || VagueMessages.Contains(body);
    }

    /// <summary>
    /// 0 for a vague message; otherwise 60 base, +20 for a subject of conventional length
    /// (15–72 characters), +20 for a Conventional Commits prefix.
    /// </summary>
    public static int Score(string subject)
    {
        if (IsVague(subject))
        {
            return 0;
        }

        var trimmed = subject.Trim();
        var points = 60;
        if (trimmed.Length is >= IdealMinLength and <= IdealMaxLength)
        {
            points += 20;
        }

        if (ConventionalPrefix().IsMatch(trimmed))
        {
            points += 20;
        }

        return points;
    }

    private static string Normalize(string subject) =>
        subject.Trim().TrimEnd('.', '!', ' ').ToLowerInvariant();

    [GeneratedRegex(@"^[a-z]+(\([^)]+\))?!?:\s*", RegexOptions.IgnoreCase)]
    private static partial Regex ConventionalPrefix();
}
