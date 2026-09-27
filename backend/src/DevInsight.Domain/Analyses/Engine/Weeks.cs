namespace DevInsight.Domain.Analyses.Engine;

public static class Weeks
{
    /// <summary>The Monday (UTC) of the week containing <paramref name="moment"/>.</summary>
    public static DateOnly StartOf(DateTimeOffset moment)
    {
        var date = DateOnly.FromDateTime(moment.UtcDateTime);
        var offset = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-offset);
    }
}
