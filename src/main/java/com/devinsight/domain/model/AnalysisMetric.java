package com.devinsight.domain.model;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.UUID;

/**
 * A single named metric collected during a repository analysis.
 *
 * Metrics are the raw building blocks used to compute the scored dimensions
 * on an {@link Analysis}. Some metrics are included directly in the score
 * calculation; others are recorded for informational/debug purposes only.
 *
 * Examples:
 *   name="commit_count_last_30d", value=47, includedInScore=true
 *   name="open_issues",           value=12, includedInScore=false
 */
@Data
@Builder
@AllArgsConstructor
@NoArgsConstructor
public class AnalysisMetric {

    /** Unique internal identifier for this metric record. */
    private UUID id;

    /** Foreign key to the parent {@link Analysis}. */
    private UUID analysisId;

    /** Human-readable metric key (snake_case convention). */
    private String name;

    /** Numeric value of the metric. */
    private double value;

    /** Whether this metric is fed into any score calculation. */
    private boolean includedInScore;
}
