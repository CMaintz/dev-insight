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
        using var process = Process.Start(StartInfo(workingDirectory, arguments, environment))
            ?? throw new InvalidOperationException("Could not start git.");
        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);
        await WaitOrKillAsync(process, cancellationToken);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {Subcommand(arguments)} failed ({process.ExitCode}): {(await stderr).Trim()}");
        }

        return await stdout;
    }

    private ProcessStartInfo StartInfo(
        string workingDirectory, IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> environment)
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
        arguments.ToList().ForEach(startInfo.ArgumentList.Add);

        // Never block on a credential prompt; fail instead.
        startInfo.Environment["GIT_TERMINAL_PROMPT"] = "0";
        startInfo.Environment["GCM_INTERACTIVE"] = "never";
        foreach (var (key, value) in environment)
        {
            startInfo.Environment[key] = value;
        }

        return startInfo;
    }

    private static async Task WaitOrKillAsync(Process process, CancellationToken cancellationToken)
    {
        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }
    }

    private static string Subcommand(IReadOnlyList<string> arguments) =>
        arguments.First(a => !a.StartsWith('-') && !a.Contains('='));
}
