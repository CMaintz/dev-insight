using System.Reflection;
using NetArchTest.Rules;

namespace DevInsight.ArchitectureTests;

/// <summary>Hexagonal architecture, enforced: dependencies point inward, toward the domain.</summary>
public class LayeringTests
{
    private static readonly Assembly Domain = typeof(Domain.Analyses.Analysis).Assembly;
    private static readonly Assembly Application = typeof(Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_depends_only_on_the_base_library() =>
        AssertNoDependency(Domain,
            "DevInsight.Application", "DevInsight.Infrastructure", "DevInsight.Api",
            "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Microsoft.Extensions", "Octokit", "Anthropic", "Npgsql");

    [Fact]
    public void Application_does_not_know_about_adapters() =>
        AssertNoDependency(Application,
            "DevInsight.Infrastructure", "DevInsight.Api",
            "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Octokit", "Anthropic", "Npgsql");

    [Fact]
    public void Infrastructure_does_not_depend_on_the_web_layer() =>
        AssertNoDependency(Infrastructure, "DevInsight.Api");

    [Fact]
    public void Adapters_are_hidden_behind_ports() =>
        Types.InAssembly(Infrastructure)
            .That().HaveNameEndingWith("Store").Or().HaveNameEndingWith("Gateway").Or().HaveNameEndingWith("Source")
            .Should().NotBePublic()
            .GetResult().IsSuccessful.ShouldBeTrue();

    [Fact]
    public void Domain_entities_have_no_public_setters()
    {
        var offenders = Domain.GetTypes()
            .Where(t => t.IsClass && t.Namespace?.StartsWith("DevInsight.Domain", StringComparison.Ordinal) == true)
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => p.SetMethod?.IsPublic == true && !IsInitOnly(p))
                .Select(p => $"{t.Name}.{p.Name}"))
            .ToList();
        offenders.ShouldBeEmpty();
    }

    private static bool IsInitOnly(PropertyInfo property) =>
        property.SetMethod!.ReturnParameter.GetRequiredCustomModifiers()
            .Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit");

    private static void AssertNoDependency(Assembly assembly, params string[] forbidden)
    {
        var result = Types.InAssembly(assembly).ShouldNot().HaveDependencyOnAny(forbidden).GetResult();
        (result.FailingTypeNames ?? []).ShouldBeEmpty();
    }
}
