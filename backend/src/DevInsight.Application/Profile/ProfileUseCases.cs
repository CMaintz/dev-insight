using DevInsight.Application.Abstractions;
using DevInsight.Application.Common;
using DevInsight.Domain.Users;

namespace DevInsight.Application.Profile;

public sealed record ProfileUpdate(string? Bio, string? LinkedInUrl, bool IsPortfolioPublic);

public sealed class ProfileUseCases(IUserStore users, IUnitOfWork unitOfWork, TimeProvider clock)
{
    public async Task<User> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        await users.GetAsync(userId, cancellationToken) ?? throw new NotFoundException("User", userId);

    public async Task<User> UpdateAsync(Guid userId, ProfileUpdate update, CancellationToken cancellationToken)
    {
        var user = await GetAsync(userId, cancellationToken);
        user.UpdateProfile(update.Bio, update.LinkedInUrl, update.IsPortfolioPublic, clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return user;
    }
}
