namespace DevInsight.Domain.Analyses;

public enum AnalysisScope
{
    /// <summary>The whole repository.</summary>
    Repo,

    /// <summary>Only the commits (and files touched) by the analysed user.</summary>
    UserContribution,
}

public enum MetricCategory
{
    Activity,
    CommitQuality,
    Structure,
    Quality,
}
