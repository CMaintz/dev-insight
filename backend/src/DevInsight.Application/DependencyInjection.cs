using DevInsight.Application.Analyses;
using DevInsight.Application.Auth;
using DevInsight.Application.Dashboard;
using DevInsight.Application.Portfolio;
using DevInsight.Application.Profile;
using DevInsight.Application.Projects;
using DevInsight.Application.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DevInsight.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<LoginWithGitHub>();
        services.AddScoped<ImportRepositories>();
        services.AddScoped<RepositoryQueries>();
        services.AddScoped<RequestAnalysis>();
        services.AddScoped<RunAnalysis>();
        services.AddScoped<AnalysisQueries>();
        services.AddScoped<GetDashboard>();
        services.AddScoped<GetPortfolio>();
        services.AddScoped<ProfileUseCases>();
        services.AddScoped<ProjectUseCases>();
        return services;
    }
}
