using DevInsight.Domain.Analyses.Engine;
using DevInsight.Domain.Users;

namespace DevInsight.Domain.Tests;

internal static class TestData
{
    public static readonly DateTimeOffset Now = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);
    public const string UserEmail = "dev@example.com";
    public const string OtherEmail = "someone@example.com";

    public static ContributorIdentity Identity => new("dev", [UserEmail]);

    public static CommitRecord Commit(
        int daysAgo,
        string subject = "feat: add repository import endpoint",
        string email = UserEmail,
        int additions = 20,
        int deletions = 5,
        bool isMerge = false,
        params string[] paths) =>
        new(
            Sha: Guid.NewGuid().ToString("N"),
            AuthorName: email.Split('@')[0],
            AuthorEmail: email,
            AuthoredAt: Now.AddDays(-daysAgo),
            Subject: subject,
            Additions: additions,
            Deletions: deletions,
            IsMerge: isMerge,
            Paths: paths.Length == 0 ? ["src/app.ts"] : paths);

    public static SourceFile File(string path, int lines) => new(path, lines);

    /// <summary>A small, healthy project: README, lint + CI config, tests, and focused files.</summary>
    public static IReadOnlyList<SourceFile> HealthyFiles() =>
    [
        File("README.md", 80),
        File(".editorconfig", 20),
        File(".github/workflows/ci.yml", 40),
        File("LICENSE", 21),
        File("src/app/api.ts", 120),
        File("src/app/models.ts", 60),
        File("src/app/service.ts", 180),
        File("src/app/util.ts", 40),
        File("src/app/api.spec.ts", 90),
        File("src/app/service.spec.ts", 110),
    ];

    /// <summary>One commit per week for <paramref name="weeks"/> weeks, most recent yesterday.</summary>
    public static List<CommitRecord> WeeklyCommits(int weeks, string email = UserEmail) =>
        [.. Enumerable.Range(0, weeks).Select(w => Commit(daysAgo: 1 + (w * 7), email: email))];
}
