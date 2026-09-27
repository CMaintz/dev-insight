using DevInsight.Application.Abstractions;
using DevInsight.Domain.Users;
using Microsoft.Extensions.Options;
using Octokit;
using DomainRepository = DevInsight.Domain.Repositories.GitHubRepositoryInfo;

namespace DevInsight.Infrastructure.GitHub;

/// <summary>GitHub adapter built on Octokit. A client is created per call with the user's own token.</summary>
internal sealed class OctokitGitHubGateway(IOptions<GitHubOptions> options) : IGitHubGateway
{
    private readonly GitHubOptions _options = options.Value;

    public Uri GetAuthorizationUrl(string state)
    {
        var request = new OauthLoginRequest(_options.ClientId)
        {
            State = state,
            RedirectUri = new Uri(_options.CallbackUrl),
            AllowSignup = true,
        };
        request.Scopes.Add("read:user");
        request.Scopes.Add("user:email");
        if (_options.IncludePrivateRepositories)
        {
            request.Scopes.Add("repo");
        }

        return Anonymous().Oauth.GetGitHubLoginUrl(request);
    }

    public async Task<string> ExchangeCodeForTokenAsync(string code, CancellationToken cancellationToken)
    {
        var request = new OauthTokenRequest(_options.ClientId, _options.ClientSecret, code)
        {
            RedirectUri = new Uri(_options.CallbackUrl),
        };
        var token = await Anonymous().Oauth.CreateAccessToken(request);
        return string.IsNullOrEmpty(token.AccessToken)
            ? throw new InvalidOperationException($"GitHub rejected the OAuth code: {token.ErrorDescription ?? token.Error}")
            : token.AccessToken;
    }

    public async Task<GitHubIdentity> GetIdentityAsync(string accessToken, CancellationToken cancellationToken)
    {
        var client = Authenticated(accessToken);
        var user = await client.User.Current();
        var emails = await client.User.Email.GetAll();
        return new GitHubIdentity(
            user.Id,
            user.Login,
            user.Name,
            user.Email,
            user.AvatarUrl,
            [.. emails.Where(e => e.Verified).Select(e => e.Email)]);
    }

    public async Task<IReadOnlyList<DomainRepository>> ListRepositoriesAsync(string accessToken, CancellationToken cancellationToken)
    {
        var request = new RepositoryRequest
        {
            Affiliation = RepositoryAffiliation.Owner | RepositoryAffiliation.Collaborator | RepositoryAffiliation.OrganizationMember,
            Sort = RepositorySort.Pushed,
            Direction = SortDirection.Descending,
        };
        var repositories = await Authenticated(accessToken).Repository.GetAllForCurrent(request);
        return [.. repositories.Where(r => _options.IncludePrivateRepositories || !r.Private).Select(Map)];
    }

    public async Task<IReadOnlyDictionary<string, long>> GetLanguagesAsync(
        string accessToken, string owner, string name, CancellationToken cancellationToken)
    {
        var languages = await Authenticated(accessToken).Repository.GetAllLanguages(owner, name);
        return languages.ToDictionary(l => l.Name, l => l.NumberOfBytes);
    }

    private static DomainRepository Map(Octokit.Repository r) => new(
        r.Id,
        r.Owner.Login,
        r.Name,
        r.Description,
        r.HtmlUrl,
        r.Language,
        r.StargazersCount,
        r.ForksCount,
        r.Fork,
        r.Private,
        r.Archived,
        r.DefaultBranch ?? "main",
        r.Size,
        r.PushedAt ?? r.UpdatedAt);

    private GitHubClient Anonymous() => new(new ProductHeaderValue(_options.ProductName));

    private GitHubClient Authenticated(string accessToken) =>
        new(new ProductHeaderValue(_options.ProductName)) { Credentials = new Credentials(accessToken) };
}
