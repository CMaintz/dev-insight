namespace DevInsight.Application.Common;

/// <summary>
/// The requested resource does not exist, or belongs to another user — the two are deliberately
/// indistinguishable so IDs of other users' resources cannot be probed.
/// </summary>
public sealed class NotFoundException(string resource, object id)
    : Exception($"{resource} '{id}' was not found.");

public static class Ownership
{
    /// <summary>The entity if it exists and belongs to <paramref name="userId"/>; otherwise <see cref="NotFoundException"/>.</summary>
    public static T OwnedBy<T>(this T? entity, Guid userId, string resource, Guid id)
        where T : class, Domain.Common.IUserOwned =>
        entity is not null && entity.UserId == userId ? entity : throw new NotFoundException(resource, id);
}

/// <summary>The operation needs something the user has not set up yet (e.g. a GitHub token).</summary>
public sealed class PreconditionFailedException(string message) : Exception(message);
