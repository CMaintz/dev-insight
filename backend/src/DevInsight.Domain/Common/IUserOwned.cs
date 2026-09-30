namespace DevInsight.Domain.Common;

/// <summary>An entity that belongs to exactly one user.</summary>
public interface IUserOwned
{
    Guid UserId { get; }
}
