using DevInsight.Application.Abstractions;
using DevInsight.Domain.Users;

namespace DevInsight.Application.Auth;

/// <summary>UC1: exchange a GitHub OAuth code, create or update the user, and keep their token for imports.</summary>
public sealed class LoginWithGitHub(
    IGitHubGateway gitHub,
    IUserStore users,
    IGitHubCredentialStore credentials,
    IUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<User> ExecuteAsync(string code, CancellationToken cancellationToken)
    {
        var accessToken = await gitHub.ExchangeCodeForTokenAsync(code, cancellationToken);
        var identity = await gitHub.GetIdentityAsync(accessToken, cancellationToken);
        var now = clock.GetUtcNow();

        var user = await users.FindByGitHubIdAsync(identity.GitHubId, cancellationToken);
        if (user is null)
        {
            user = User.Register(identity, now);
            users.Add(user);
        }
        else
        {
            user.SyncWith(identity, now);
        }

        await credentials.SaveAsync(user.Id, accessToken, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return user;
    }
}
