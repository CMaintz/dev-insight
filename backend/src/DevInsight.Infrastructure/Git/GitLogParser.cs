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

    public static IReadOnlyList<CommitRecord> Parse(string output)
    {
        var commits = new List<CommitRecord>();
        foreach (var record in output.Split('\u001e', StringSplitOptions.RemoveEmptyEntries))
        {
            var lines = record.Split('\n');
            var header = lines[0].Split('\u001f');
            if (header.Length < 6)
            {
                continue;
            }

            var additions = 0;
            var deletions = 0;
            var paths = new List<string>();
            foreach (var line in lines.Skip(1))
            {
                var parts = line.Split('\t');
                if (parts.Length != 3)
                {
                    continue;
                }

                // Binary files report "-" for both counts.
                additions += int.TryParse(parts[0], out var added) ? added : 0;
                deletions += int.TryParse(parts[1], out var deleted) ? deleted : 0;
                paths.Add(parts[2].Trim());
            }

            var parents = header[4].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            commits.Add(new CommitRecord(
                Sha: header[0],
                AuthorName: header[1],
                AuthorEmail: header[2].ToLowerInvariant(),
                AuthoredAt: DateTimeOffset.Parse(header[3], CultureInfo.InvariantCulture),
                Subject: header[5].TrimEnd('\r'),
                Additions: additions,
                Deletions: deletions,
                IsMerge: parents.Length > 1,
                Paths: paths));
        }

        return commits;
    }
}
