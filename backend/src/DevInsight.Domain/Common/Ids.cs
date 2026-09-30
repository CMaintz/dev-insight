namespace DevInsight.Domain.Common;

public static class Ids
{
    /// <summary>Time-ordered UUIDv7 — index-friendly primary keys.</summary>
    public static Guid New() => Guid.CreateVersion7();
}
