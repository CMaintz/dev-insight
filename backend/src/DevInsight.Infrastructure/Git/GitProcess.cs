using System.Diagnostics;
using System.Text;

namespace DevInsight.Infrastructure.Git;

/// <summary>Runs the git CLI with an argument list (never a shell string) and captures its output.</summary>
internal sealed class GitProcess(string executable)
{
    public async Task<string> RunAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        IReadOnlyDictionary<string, string> environment,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo(executable)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // Never block on a credential prompt; fail instead.
        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
        startInfo.Environment["GCM_INTERACTIVE"] = "never";
        foreach (var (key, value) in environment)
        {
            startInfo.Environment[key] = value;
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start git.");
        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        if (process.ExitCode != 0)
        {
            var error = (await stderr).Trim();
            throw new InvalidOperationException($"git {arguments.First(a => !a.StartsWith('-') && !a.Contains('='))} failed ({process.ExitCode}): {error}");
        }

        return await stdout;
    }
}
