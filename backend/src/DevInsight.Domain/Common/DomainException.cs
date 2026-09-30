namespace DevInsight.Domain.Common;

/// <summary>A domain invariant was violated by the caller's input.</summary>
public sealed class DomainException(string message) : Exception(message);
