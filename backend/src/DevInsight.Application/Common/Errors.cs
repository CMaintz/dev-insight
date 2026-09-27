namespace DevInsight.Application.Common;

/// <summary>
/// The requested resource does not exist, or belongs to another user — the two are deliberately
/// indistinguishable so IDs of other users' resources cannot be probed.
/// </summary>
public sealed class NotFoundException(string resource, object id)
    : Exception($"{resource} '{id}' was not found.");

/// <summary>The operation needs something the user has not set up yet (e.g. a GitHub token).</summary>
public sealed class PreconditionFailedException(string message) : Exception(message);
