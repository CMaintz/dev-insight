namespace DevInsight.Domain.Analyses.Engine;

/// <summary>Path-based heuristics: what kind of file is this?</summary>
public static class FileClassifier
{
    private static readonly HashSet<string> SourceExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".fs", ".vb", ".java", ".kt", ".kts", ".scala", ".groovy", ".ts", ".tsx", ".js", ".jsx",
        ".mjs", ".cjs", ".vue", ".svelte", ".py", ".rb", ".php", ".go", ".rs", ".swift", ".m", ".mm",
        ".c", ".h", ".cc", ".cpp", ".hpp", ".cxx", ".dart", ".ex", ".exs", ".erl", ".hs", ".clj",
        ".lua", ".r", ".jl", ".sh", ".ps1", ".sql", ".html", ".css", ".scss", ".sass", ".less",
    };

    private static readonly string[] IgnoredSegments =
    [
        "node_modules/", "vendor/", "dist/", "build/", "out/", "bin/", "obj/", "target/", ".git/",
        "coverage/", "__pycache__/", ".venv/", "venv/", "third_party/", "wwwroot/lib/",
        "Migrations/", "migrations/", "generated/", ".angular/", ".next/",
    ];

    private static readonly string[] TestMarkers =
    [
        "/test/", "/tests/", "/__tests__/", "/spec/", "/specs/", ".test.", ".spec.", "_test.", "_spec.",
        ".tests/", ".test/", "/testing/",
    ];

    private static readonly string[] LintConfigNames =
    [
        ".eslintrc", "eslint.config.", ".prettierrc", "prettier.config.", ".editorconfig", ".stylelintrc",
        "checkstyle", "pmd", "detekt", ".ktlint", "ruff.toml", ".flake8", ".pylintrc", "pyproject.toml",
        ".rubocop.yml", "phpcs.xml", "phpstan.neon", ".golangci", "rustfmt.toml", "clippy.toml",
        ".swiftlint.yml", "stylecop.json", ".clang-format", "biome.json", "tslint.json", ".habit-hooks/",
    ];

    public static bool IsIgnored(string path)
    {
        var normalized = "/" + path;
        return IgnoredSegments.Any(segment => normalized.Contains("/" + segment, StringComparison.Ordinal))
            || path.EndsWith(".min.js", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".min.css", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".designer.cs", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSource(string path) =>
        !IsIgnored(path) && SourceExtensions.Contains(Path.GetExtension(path));

    public static bool IsTest(string path)
    {
        if (!IsSource(path))
        {
            return false;
        }

        var normalized = "/" + path.ToLowerInvariant();
        var fileName = Path.GetFileNameWithoutExtension(path);
        return TestMarkers.Any(marker => normalized.Contains(marker, StringComparison.Ordinal))
            || fileName.EndsWith("Test", StringComparison.Ordinal)
            || fileName.EndsWith("Tests", StringComparison.Ordinal)
            || fileName.StartsWith("test_", StringComparison.Ordinal)
            || fileName.Equals("test", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsReadme(string path) =>
        !path.Contains('/', StringComparison.Ordinal)
        && Path.GetFileNameWithoutExtension(path).Equals("readme", StringComparison.OrdinalIgnoreCase);

    public static bool IsLicense(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        return !path.Contains('/', StringComparison.Ordinal)
            && (name.Equals("license", StringComparison.OrdinalIgnoreCase)
                || name.Equals("licence", StringComparison.OrdinalIgnoreCase)
                || name.Equals("copying", StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsLintConfig(string path)
    {
        var lower = path.ToLowerInvariant();
        var fileName = Path.GetFileName(lower);
        return LintConfigNames.Any(name => fileName.StartsWith(name, StringComparison.Ordinal)
            || lower.StartsWith(name, StringComparison.Ordinal));
    }

    public static bool IsCiConfig(string path) =>
        path.StartsWith(".github/workflows/", StringComparison.Ordinal)
        || path == ".gitlab-ci.yml"
        || path == "azure-pipelines.yml"
        || path == "Jenkinsfile"
        || path.StartsWith(".circleci/", StringComparison.Ordinal)
        || path == ".travis.yml"
        || path == "bitbucket-pipelines.yml";

    /// <summary>Folder depth of a path: "a.cs" is 0, "src/a.cs" is 1.</summary>
    public static int Depth(string path) => path.Count(c => c == '/');
}
