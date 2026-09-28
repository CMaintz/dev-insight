using System.Diagnostics;
using DevInsight.Domain.Repositories;
using DevInsight.Infrastructure.Git;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DevInsight.Infrastructure.Tests;

public class GitLogParserTests
{
    [Fact]
    public void Parses_git_log_records()
    {
        const string log =
            "\u001eaaa\u001fAda\u001fADA@Example.com\u001f2026-09-01T10:00:00+02:00\u001f\u001ffeat: first\n\n"
            + "10\t2\tsrc/a.cs\n-\t-\tlogo.png\n"
            + "\u001ebbb\u001fBob\u001fbob@example.com\u001f2026-09-02T10:00:00+00:00\u001fp1 p2\u001fMerge branch 'x'\n";

        var commits = GitLogParser.Parse(log);

        commits.Count.ShouldBe(2);
        var first = commits[0];
        (first.Sha, first.AuthorEmail, first.Subject, first.Additions, first.Deletions, first.IsMerge)
            .ShouldBe(("aaa", "ada@example.com", "feat: first", 10, 2, false));
        first.Paths.ShouldBe(["src/a.cs", "logo.png"]);
        first.AuthoredAt.ShouldBe(new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero));
        commits[1].IsMerge.ShouldBeTrue();
    }

    [Fact]
    public void Ignores_malformed_records() => GitLogParser.Parse("\u001egarbage\n").ShouldBeEmpty();
}

/// <summary>Clones a real local repository through the git CLI — the adapter end to end, minus GitHub.</summary>
public sealed class GitSnapshotSourceTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "devinsight-tests", Guid.NewGuid().ToString("N"));

    private string WorkRoot => Path.Combine(_root, "work");

    [Fact]
    public async Task Captures_commit_history_with_line_stats()
    {
        var snapshot = await CaptureDemoRepositoryAsync();

        snapshot.HeadCommitSha.ShouldNotBeNullOrWhiteSpace();
        snapshot.Commits.Select(c => c.Subject).ShouldBe(["Add service", "Initial commit"]);
        snapshot.Commits[0].Paths.ShouldBe(["src/Service.cs"]);
        snapshot.Commits[0].Additions.ShouldBe(3);
    }

    [Fact]
    public async Task Captures_files_at_head_with_line_counts() =>
        (await CaptureDemoRepositoryAsync()).Files.Select(f => (f.Path, f.Lines))
            .ShouldBe([("README.md", 1), ("src/Service.cs", 3)], ignoreOrder: true);

    [Fact]
    public async Task Clone_is_deleted_after_capture()
    {
        await CaptureDemoRepositoryAsync();
        Directory.EnumerateDirectories(Path.Combine(WorkRoot, "devinsight")).ShouldBeEmpty();
    }

    private async Task<Domain.Analyses.Engine.RepositorySnapshot> CaptureDemoRepositoryAsync()
    {
        CreateRepository(Path.Combine(_root, "octo", "demo.git"));
        var source = new GitSnapshotSource(
            Options.Create(new GitAnalysisOptions { CloneBaseUrl = new Uri(_root).AbsoluteUri, WorkRoot = WorkRoot }),
            NullLogger<GitSnapshotSource>.Instance);
        var repository = Repository.Import(Guid.NewGuid(), "octo",
            new GitHubRepositoryInfo(1, "octo", "demo", null, "https://github.com/octo/demo", "C#", 0, 0, false, false, false, "main", 1, null), DateTimeOffset.UtcNow);
        return await source.CaptureAsync(repository, "unused-token", TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Oversized_repository_is_rejected_before_cloning()
    {
        var source = new GitSnapshotSource(Options.Create(new GitAnalysisOptions { MaxRepositorySizeMb = 1 }), NullLogger<GitSnapshotSource>.Instance);
        var repository = Repository.Import(Guid.NewGuid(), "octo",
            new GitHubRepositoryInfo(1, "octo", "big", null, "u", null, 0, 0, false, false, false, "main", 5000, null), DateTimeOffset.UtcNow);

        var error = await Should.ThrowAsync<InvalidOperationException>(() => source.CaptureAsync(repository, "t", TestContext.Current.CancellationToken));
        error.Message.ShouldContain("limit is 1 MB");
    }

    public void Dispose()
    {
        if (!Directory.Exists(_root))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(_root, recursive: true);
    }

    private static void CreateRepository(string path)
    {
        Directory.CreateDirectory(path);
        Git(path, "init", "--quiet", "--initial-branch=main");
        File.WriteAllText(Path.Combine(path, "README.md"), "# Demo\n");
        Git(path, "add", ".");
        Git(path, "commit", "--quiet", "-m", "Initial commit");
        Directory.CreateDirectory(Path.Combine(path, "src"));
        File.WriteAllText(Path.Combine(path, "src", "Service.cs"), "class A\n{\n}\n");
        Git(path, "add", ".");
        Git(path, "commit", "--quiet", "-m", "Add service");
    }

    private static void Git(string directory, params string[] args)
    {
        var info = new ProcessStartInfo("git") { WorkingDirectory = directory, RedirectStandardError = true, UseShellExecute = false };
        foreach (var arg in new[] { "-c", "user.name=Test", "-c", "user.email=test@example.com", "-c", "commit.gpgsign=false" }.Concat(args))
        {
            info.ArgumentList.Add(arg);
        }

        using var process = Process.Start(info)!;
        process.WaitForExit();
        process.ExitCode.ShouldBe(0, process.StandardError.ReadToEnd());
    }
}
