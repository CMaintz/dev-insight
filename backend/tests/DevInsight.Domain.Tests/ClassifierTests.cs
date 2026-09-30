using DevInsight.Domain.Analyses.Engine;

namespace DevInsight.Domain.Tests;

public class CommitMessageClassifierTests
{
    [Theory]
    [InlineData("fix")]
    [InlineData("Fix.")]
    [InlineData("update")]
    [InlineData("stuff")]
    [InlineData("fixed stuff")]
    [InlineData("wip")]
    [InlineData("fix: stuff")]
    [InlineData("chore: update")]
    [InlineData("asdf")]
    public void Vague_messages_are_detected(string subject) =>
        CommitMessageClassifier.IsVague(subject).ShouldBeTrue();

    [Theory]
    [InlineData("Add repository import endpoint")]
    [InlineData("fix: prevent duplicate import on re-login")]
    [InlineData("Refactor scoring into dimension analysers")]
    public void Descriptive_messages_are_not_vague(string subject) =>
        CommitMessageClassifier.IsVague(subject).ShouldBeFalse();

    [Fact]
    public void Vague_message_scores_zero() => CommitMessageClassifier.Score("update").ShouldBe(0);

    [Fact]
    public void Conventional_message_of_ideal_length_scores_full() =>
        CommitMessageClassifier.Score("feat(api): add portfolio endpoint").ShouldBe(100);

    [Fact]
    public void Plain_descriptive_message_of_ideal_length_scores_eighty() =>
        CommitMessageClassifier.Score("Add portfolio endpoint").ShouldBe(80);

    [Fact]
    public void Overlong_subject_loses_length_points() =>
        CommitMessageClassifier.Score(new string('a', 30) + " " + new string('b', 60)).ShouldBe(60);
}

public class FileClassifierTests
{
    [Theory]
    [InlineData("src/app/service.spec.ts")]
    [InlineData("tests/DevInsight.Domain.Tests/ScoringTests.cs")]
    [InlineData("src/test/java/com/acme/FooTest.java")]
    [InlineData("pkg/handler_test.go")]
    [InlineData("tests/test_models.py")]
    [InlineData("__tests__/button.jsx")]
    public void Test_files_are_recognised(string path) => FileClassifier.IsTest(path).ShouldBeTrue();

    [Theory]
    [InlineData("src/app/service.ts")]
    [InlineData("src/Contest.cs")]
    [InlineData("README.md")]
    public void Non_test_files_are_not_tests(string path) => FileClassifier.IsTest(path).ShouldBeFalse();

    [Theory]
    [InlineData("node_modules/lodash/index.js")]
    [InlineData("frontend/dist/main.js")]
    [InlineData("src/Migrations/20260101_Init.cs")]
    [InlineData("wwwroot/lib/jquery.min.js")]
    [InlineData("obj/Debug/App.g.cs")]
    public void Vendored_or_generated_files_are_ignored(string path)
    {
        FileClassifier.IsIgnored(path).ShouldBeTrue();
        FileClassifier.IsSource(path).ShouldBeFalse();
    }

    [Fact]
    public void Only_root_readme_counts() =>
        (FileClassifier.IsReadme("README.md"), FileClassifier.IsReadme("docs/README.md")).ShouldBe((true, false));

    [Theory]
    [InlineData("eslint.config.js")]
    [InlineData(".editorconfig")]
    [InlineData("frontend/.prettierrc")]
    [InlineData("config/checkstyle/checkstyle.xml")]
    public void Lint_configs_are_recognised(string path) => FileClassifier.IsLintConfig(path).ShouldBeTrue();

    [Theory]
    [InlineData(".github/workflows/ci.yml", true)]
    [InlineData(".gitlab-ci.yml", true)]
    [InlineData("docs/ci.md", false)]
    public void Ci_configs_are_recognised(string path, bool expected) => FileClassifier.IsCiConfig(path).ShouldBe(expected);

    [Theory]
    [InlineData("a.cs", 0)]
    [InlineData("src/a.cs", 1)]
    [InlineData("src/app/core/a.ts", 3)]
    public void Depth_counts_folders(string path, int depth) => FileClassifier.Depth(path).ShouldBe(depth);
}
