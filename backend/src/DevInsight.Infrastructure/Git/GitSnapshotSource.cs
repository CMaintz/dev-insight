using System.Text;
using DevInsight.Application.Abstractions;
using DevInsight.Domain.Analyses.Engine;
using DevInsight.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevInsight.Infrastructure.Git;

public sealed class GitAnalysisOptions
{
    public const string Section = "GitAnalysis";

    public string GitExecutable { get; set; } = "git";

    /// <summary>Base URL repositories are cloned from; overridable for tests and GitHub Enterprise.</summary>
    public string CloneBaseUrl { get; set; } = "https://github.com";

    /// <summary>Where clones are made; defaults to the system temp directory.</summary>
    public string? WorkRoot { get; set; }

    /// <summary>Repositories larger than this (GitHub-reported size) are rejected rather than cloned.</summary>
    public int MaxRepositorySizeMb { get; set; } = 500;

    /// <summary>Only the most recent commits are analysed, bounding time and memory.</summary>
    public int MaxCommits { get; set; } = 5000;

    public int CloneTimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// Captures a snapshot by cloning the repository into a temporary directory with the git CLI.
/// One clone replaces thousands of per-commit GitHub API calls (and their rate limit), and gives
/// exact per-commit line stats and HEAD file sizes. The clone is deleted afterwards.
/// </summary>
internal sealed class GitSnapshotSource(IOptions<GitAnalysisOptions> options, ILogger<GitSnapshotSource> logger)
    : IRepositorySnapshotSource
{
    private const int MaxFileBytes = 2 * 1024 * 1024;
    private const int BinarySniffBytes = 8000;

    private static readonly Dictionary<string, string> NoEnv = [];

    private readonly GitAnalysisOptions _options = options.Value;
    private readonly GitProcess _git = new(options.Value.GitExecutable);

    public async Task<RepositorySnapshot> CaptureAsync(Repository repository, string accessToken, CancellationToken cancellationToken)
    {
        if (repository.SizeKb > _options.MaxRepositorySizeMb * 1024L)
        {
            throw new InvalidOperationException(
                $"{repository.FullName} is {repository.SizeKb / 1024} MB; the limit is {_options.MaxRepositorySizeMb} MB.");
        }

        var directory = Path.Combine(_options.WorkRoot ?? Path.GetTempPath(), "devinsight", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(_options.CloneTimeoutSeconds));
            await CloneAsync(repository, accessToken, directory, timeout.Token);

            var head = (await _git.RunAsync(directory, ["rev-parse", "HEAD"], NoEnv, cancellationToken)).Trim();
            var log = await _git.RunAsync(directory,
                ["-c", "core.quotePath=false", "log", $"--max-count={_options.MaxCommits}", "--no-renames", "--numstat", $"--format={GitLogParser.Format}"],
                NoEnv, cancellationToken);
            var files = await ReadFilesAsync(directory, cancellationToken);
            return new RepositorySnapshot(head, GitLogParser.Parse(log), files);
        }
        finally
        {
            DeleteDirectory(directory);
        }
    }

    private async Task CloneAsync(Repository repository, string accessToken, string directory, CancellationToken cancellationToken)
    {
        // The token travels as an HTTP header via GIT_CONFIG_* environment variables, so it never
        // appears in the process arguments or in the clone's .git/config.
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"x-access-token:{accessToken}"));
        var environment = new Dictionary<string, string>
        {
            ["GIT_CONFIG_COUNT"] = "1",
            ["GIT_CONFIG_KEY_0"] = "http.https://github.com/.extraheader",
            ["GIT_CONFIG_VALUE_0"] = $"AUTHORIZATION: basic {basic}",
        };
        var url = $"{_options.CloneBaseUrl.TrimEnd('/')}/{repository.Owner}/{repository.Name}.git";
        await _git.RunAsync(directory,
            ["clone", "--quiet", "--no-tags", "--single-branch", "--branch", repository.DefaultBranch, url, "."],
            environment, cancellationToken);
    }

    private async Task<IReadOnlyList<SourceFile>> ReadFilesAsync(string directory, CancellationToken cancellationToken)
    {
        var listing = await _git.RunAsync(directory, ["ls-files", "-z"], NoEnv, cancellationToken);
        var files = new List<SourceFile>();
        foreach (var path in listing.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            var lines = await CountLinesAsync(Path.Combine(directory, path), cancellationToken);
            if (lines is not null)
            {
                files.Add(new SourceFile(path, lines.Value));
            }
        }

        return files;
    }

    /// <summary>Line count of a text file; null for binary, oversized or unreadable files.</summary>
    private static async Task<int?> CountLinesAsync(string fullPath, CancellationToken cancellationToken)
    {
        var info = new FileInfo(fullPath);
        if (!info.Exists || info.Length > MaxFileBytes)
        {
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
        if (bytes.AsSpan(0, Math.Min(bytes.Length, BinarySniffBytes)).Contains((byte)0))
        {
            return null;
        }

        var lines = bytes.AsSpan().Count((byte)'\n');
        return bytes.Length > 0 && bytes[^1] != (byte)'\n' ? lines + 1 : lines;
    }

    private void DeleteDirectory(string directory)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal); // git marks pack files read-only on Windows
            }

            Directory.Delete(directory, recursive: true);
        }
        catch (IOException ex)
        {
            logger.LogWarning(ex, "Could not delete clone directory {Directory}", directory);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Could not delete clone directory {Directory}", directory);
        }
    }
}
