using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DevInsight.ArchitectureTests;

/// <summary>
/// House rules, enforced: a function does one thing (at most 18 lines of actual code) and a file stays
/// cohesive (at most 300 lines). Applies to production and test code; generated migrations are exempt.
/// </summary>
public class CodeSizeTests
{
    private const int MaxFunctionCodeLines = 18;
    private const int MaxFileLines = 300;

    [Fact]
    public void Functions_have_at_most_18_lines_of_code()
    {
        var offenders = SourceFiles()
            .SelectMany(file => CodeMetrics.Functions(file).Select(f => (file, f)))
            .Where(x => x.f.CodeLines > MaxFunctionCodeLines)
            .Select(x => $"{Relative(x.file)}:{x.f.Line} {x.f.Name} ({x.f.CodeLines} lines)")
            .ToList();
        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Files_have_at_most_300_lines() =>
        SourceFiles()
            .Where(file => File.ReadAllLines(file).Length > MaxFileLines)
            .Select(Relative)
            .ShouldBeEmpty();

    [Fact]
    public void Only_code_lines_are_counted() =>
        CodeMetrics.Functions(WriteTemp("""
            class C
            {
                void M()
                {
                    // comment
                    var a = 1;

                    if (a > 0)
                    {
                        a++;
                    }
                }
            }
            """)).ShouldHaveSingleItem().CodeLines.ShouldBe(3);

    private static IEnumerable<string> SourceFiles()
    {
        var backend = BackendDirectory();
        return new[] { "src", "tests" }
            .SelectMany(dir => Directory.EnumerateFiles(Path.Combine(backend, dir), "*.cs", SearchOption.AllDirectories))
            .Where(path => !IsExcluded(Path.GetRelativePath(backend, path)));
    }

    private static bool IsExcluded(string relativePath)
    {
        var segments = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return segments.Any(s => s is "bin" or "obj" or "Migrations");
    }

    private static string BackendDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DevInsight.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("DevInsight.slnx not found above the test output.");
    }

    private static string Relative(string path) => Path.GetRelativePath(BackendDirectory(), path);

    private static string WriteTemp(string code)
    {
        var path = Path.Combine(Path.GetTempPath(), $"codesize-{Guid.NewGuid():N}.cs");
        File.WriteAllText(path, code);
        return path;
    }
}

internal sealed record FunctionSize(string Name, int Line, int CodeLines);

internal static class CodeMetrics
{
    public static IEnumerable<FunctionSize> Functions(string path)
    {
        var text = File.ReadAllText(path);
        var lines = text.Split('\n');
        var root = CSharpSyntaxTree.ParseText(text).GetRoot();
        foreach (var node in root.DescendantNodes())
        {
            if (Body(node) is not { } body)
            {
                continue;
            }

            var span = body.GetLocation().GetLineSpan();
            var codeLines = Enumerable.Range(span.StartLinePosition.Line, span.EndLinePosition.Line - span.StartLinePosition.Line + 1)
                .Count(i => IsCode(lines[i]));
            yield return new FunctionSize(Name(node), span.StartLinePosition.Line + 1, codeLines);
        }
    }

    private static SyntaxNode? Body(SyntaxNode node) => node switch
    {
        BaseMethodDeclarationSyntax m => (SyntaxNode?)m.Body ?? m.ExpressionBody,
        LocalFunctionStatementSyntax l => (SyntaxNode?)l.Body ?? l.ExpressionBody,
        AccessorDeclarationSyntax a => (SyntaxNode?)a.Body ?? a.ExpressionBody,
        PropertyDeclarationSyntax { ExpressionBody: { } expression } => expression,
        AnonymousFunctionExpressionSyntax { Body: BlockSyntax block } => block,
        _ => null,
    };

    private static string Name(SyntaxNode node) => node switch
    {
        MethodDeclarationSyntax m => m.Identifier.Text,
        ConstructorDeclarationSyntax c => $"{c.Identifier.Text} constructor",
        LocalFunctionStatementSyntax l => l.Identifier.Text,
        PropertyDeclarationSyntax p => p.Identifier.Text,
        _ => "lambda",
    };

    private static bool IsCode(string line)
    {
        var trimmed = line.Trim();
        return trimmed.Length > 0
            && !trimmed.StartsWith("//", StringComparison.Ordinal)
            && !trimmed.StartsWith("/*", StringComparison.Ordinal)
            && !trimmed.StartsWith('*')
            && !trimmed.All(c => "{}()[];,".Contains(c));
    }
}
