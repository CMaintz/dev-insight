using System.Globalization;
using DevInsight.Domain.Analyses.Engine;

namespace DevInsight.Infrastructure.Git;

/// <summary>
/// Parses <c>git log --numstat</c> output produced with <see cref="Format"/>: each commit starts
/// with a record separator (0x1E) and a header of unit-separated (0x1F) fields, followed by one
/// numstat line per changed file.
/// </summary>
internal static class GitLogParser
{
    public const string Format = "%x1e%H%x1f%an%x1f%ae%x1f%aI%x1f%P%x1f%s";
    private const int HeaderFields = 6;

    public static IReadOnlyList<CommitRecord> Parse(string output) =>
    [
        .. output.Split('\u001e', StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseRecord)
            .OfType<CommitRecord>(),
    ];

    private static CommitRecord? ParseRecord(string record)
    {
        var lines = record.Split('\n');
        var header = lines[0].Split('\u001f');
        if (header.Length < HeaderFields)
        {
            return null;
        }

        var changes = lines.Skip(1).Select(ParseNumstat).OfType<FileChange>().ToList();
        return new CommitRecord(
            Sha: header[0],
            AuthorName: header[1],
            AuthorEmail: header[2].ToLowerInvariant(),
            AuthoredAt: DateTimeOffset.Parse(header[3], CultureInfo.InvariantCulture),
            Subject: header[5].TrimEnd('\r'),
            Additions: changes.Sum(c => c.Additions),
            Deletions: changes.Sum(c => c.Deletions),
            IsMerge: header[4].Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 1,
            Paths: [.. changes.Select(c => c.Path)]);
    }

    /// <summary>One "added⇥deleted⇥path" line; binary files report "-" for both counts.</summary>
    private static FileChange? ParseNumstat(string line)
    {
        var parts = line.Split('\t');
        return parts.Length == 3
            ? new FileChange(parts[2].Trim(), CountOrZero(parts[0]), CountOrZero(parts[1]))
            : null;
    }

    private static int CountOrZero(string value) => int.TryParse(value, out var count) ? count : 0;

    private sealed record FileChange(string Path, int Additions, int Deletions);
}
