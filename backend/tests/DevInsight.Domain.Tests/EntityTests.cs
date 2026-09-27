using DevInsight.Domain.Analyses;
using DevInsight.Domain.Common;
using DevInsight.Domain.Projects;
using DevInsight.Domain.Repositories;
using DevInsight.Domain.Users;
using static DevInsight.Domain.Tests.TestData;

namespace DevInsight.Domain.Tests;

public class UserTests
{
    private static GitHubIdentity Identity(long id = 42, string login = "octo") =>
        new(id, login, "Octo Cat", "Octo@Example.com", "https://avatars/1", ["work@example.com"]);

    [Fact]
    public void Commit_emails_include_verified_and_noreply_addresses()
    {
        var user = User.Register(Identity(), Now);
        user.CommitEmails.ShouldBe(
            ["42+octo@users.noreply.github.com", "octo@example.com", "octo@users.noreply.github.com", "work@example.com"],
            ignoreOrder: true);
        user.ToContributorIdentity().Authored("WORK@example.com").ShouldBeTrue();
    }

    [Fact]
    public void New_users_portfolio_is_private_until_published() =>
        User.Register(Identity(), Now).IsPortfolioPublic.ShouldBeFalse();

    [Fact]
    public void Sync_refreshes_profile_fields()
    {
        var user = User.Register(Identity(), Now);
        user.SyncWith(Identity(login: "octo-renamed"), Now.AddDays(1));
        (user.Login, user.UpdatedAt).ShouldBe(("octo-renamed", Now.AddDays(1)));
    }

    [Fact]
    public void Sync_with_another_account_is_rejected() =>
        Should.Throw<DomainException>(() => User.Register(Identity(), Now).SyncWith(Identity(id: 7), Now));

    [Theory]
    [InlineData("https://www.linkedin.com/in/octo")]
    [InlineData("https://linkedin.com/in/octo")]
    public void LinkedIn_urls_are_accepted(string url)
    {
        var user = User.Register(Identity(), Now);
        user.UpdateProfile("  Hi  ", url, true, Now);
        (user.Bio, user.IsPortfolioPublic).ShouldBe(("Hi", true));
    }

    [Theory]
    [InlineData("http://linkedin.com/in/octo")]
    [InlineData("https://evil.com/linkedin.com")]
    [InlineData("https://notlinkedin.com/in/x")]
    [InlineData("javascript:alert(1)")]
    public void Non_LinkedIn_urls_are_rejected(string url) =>
        Should.Throw<DomainException>(() => User.Register(Identity(), Now).UpdateProfile(null, url, false, Now));

    [Fact]
    public void Overlong_bio_is_rejected() =>
        Should.Throw<DomainException>(() => User.Register(Identity(), Now).UpdateProfile(new string('x', 2001), null, false, Now));
}

public class RepositoryTests
{
    private static GitHubRepositoryInfo Info(string owner = "octo", bool fork = false, long id = 1) =>
        new(id, owner, "devinsight", null, "https://github.com/octo/devinsight", "C#", 3, 1, fork, false, false, "main", 100, Now);

    [Theory]
    [InlineData("octo", false, true)]
    [InlineData("OCTO", false, true)]
    [InlineData("octo", true, false)]
    [InlineData("some-org", false, false)]
    public void Only_owned_non_forks_are_selected_by_default(string owner, bool fork, bool selected) =>
        Repository.Import(Guid.NewGuid(), "octo", Info(owner, fork), Now).IsSelected.ShouldBe(selected);

    [Fact]
    public void Refresh_with_another_repository_is_rejected() =>
        Should.Throw<DomainException>(() => Repository.Import(Guid.NewGuid(), "octo", Info(), Now).Refresh(Info(id: 2), Now));

    [Fact]
    public void Full_name_combines_owner_and_name() =>
        Repository.Import(Guid.NewGuid(), "octo", Info(), Now).FullName.ShouldBe("octo/devinsight");
}

public class ProjectTests
{
    private static ProjectDetails Details(string name = "DevInsight", params string[] images) =>
        new(name, "  A platform  ", images, [Guid.Empty, Guid.Empty], 1);

    [Fact]
    public void Creates_trimmed_project_with_distinct_links()
    {
        var project = Project.Create(Guid.NewGuid(), Details(images: "https://img.example.com/a.png"), Now);
        (project.Description, project.LinkedRepositoryIds.Count).ShouldBe(("A platform", 1));
    }

    [Fact]
    public void Name_is_required() => Should.Throw<DomainException>(() => Project.Create(Guid.NewGuid(), Details(" "), Now));

    [Fact]
    public void Images_must_be_https() =>
        Should.Throw<DomainException>(() => Project.Create(Guid.NewGuid(), Details(images: "http://img.example.com/a.png"), Now));

    [Fact]
    public void At_most_ten_images() =>
        Should.Throw<DomainException>(() => Project.Create(
            Guid.NewGuid(), Details(images: [.. Enumerable.Range(0, 11).Select(i => $"https://img.example.com/{i}.png")]), Now));
}

public class AnalysisRunTests
{
    [Fact]
    public void Run_moves_through_its_lifecycle()
    {
        var run = AnalysisRun.Queue(Guid.NewGuid(), Guid.NewGuid(), AnalysisScope.Repo, Now);
        run.Start(Now);
        var analysisId = Guid.NewGuid();
        run.Succeed(analysisId, Now);
        (run.Status, run.AnalysisId, run.IsFinished).ShouldBe((AnalysisRunStatus.Succeeded, (Guid?)analysisId, true));
    }

    [Fact]
    public void Only_a_queued_run_can_start()
    {
        var run = AnalysisRun.Queue(Guid.NewGuid(), Guid.NewGuid(), AnalysisScope.Repo, Now);
        run.Start(Now);
        Should.Throw<DomainException>(() => run.Start(Now));
    }

    [Fact]
    public void Failure_message_is_truncated()
    {
        var run = AnalysisRun.Queue(Guid.NewGuid(), Guid.NewGuid(), AnalysisScope.Repo, Now);
        run.Fail(new string('x', 5000), Now);
        run.Error!.Length.ShouldBe(1000);
    }

    [Fact]
    public void Interrupted_run_is_requeued()
    {
        var run = AnalysisRun.Queue(Guid.NewGuid(), Guid.NewGuid(), AnalysisScope.Repo, Now);
        run.Start(Now);
        run.Requeue();
        (run.Status, run.StartedAt).ShouldBe((AnalysisRunStatus.Queued, (DateTimeOffset?)null));
    }
}
