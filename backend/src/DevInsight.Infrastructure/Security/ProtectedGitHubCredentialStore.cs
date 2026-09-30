using DevInsight.Application.Abstractions;
using DevInsight.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace DevInsight.Infrastructure.Security;

/// <summary>
/// Encrypts GitHub tokens with ASP.NET Core Data Protection before they touch the database. The key
/// ring itself is persisted in the same database, so it survives restarts and scales out.
/// </summary>
internal sealed class ProtectedGitHubCredentialStore(DevInsightDbContext db, IDataProtectionProvider protection, TimeProvider clock)
    : IGitHubCredentialStore
{
    private readonly IDataProtector _protector = protection.CreateProtector("DevInsight.GitHubToken.v1");

    public async Task SaveAsync(Guid userId, string accessToken, CancellationToken cancellationToken)
    {
        var credential = await db.GitHubCredentials.FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        if (credential is null)
        {
            credential = new GitHubCredential { UserId = userId };
            db.GitHubCredentials.Add(credential);
        }

        credential.ProtectedToken = _protector.Protect(accessToken);
        credential.UpdatedAt = clock.GetUtcNow();
    }

    public async Task<string?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var credential = await db.GitHubCredentials.AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);
        return credential is null ? null : _protector.Unprotect(credential.ProtectedToken);
    }
}
