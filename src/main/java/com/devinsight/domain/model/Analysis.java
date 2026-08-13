package com.devinsight.domain.model;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;
import java.util.UUID;

/**
 * Core domain model representing the result of analysing a repository.
 *
 * An analysis aggregates several scored dimensions (activity, structure, quality)
 * into a single overall score that is displayed on the developer portfolio.
 * Each analysis is scoped to either the full repository or the user's own
 * contributions (see {@link AnalysisScope}).
 */
@Data
@Builder
@AllArgsConstructor
@NoArgsConstructor
public class Analysis {

    /** Unique internal identifier for this analysis run. */
    private UUID id;

    /** Foreign key to the {@link Repository} that was analysed. */
    private UUID repositoryId;

    /** Scope that was used when running this analysis. */
    private AnalysisScope scope;

    /** Weighted aggregate of all sub-scores; range 0–100. */
    private int overallScore;

    /**
     * Activity score reflecting commit frequency, PR cadence, and recency
     * of contributions; range 0–100.
     */
    private int activityScore;

    /**
     * Structure score reflecting code organisation, directory depth,
     * presence of tests/docs, etc.; range 0–100.
     */
    private int structureScore;

    /**
     * Quality score reflecting code complexity, lint findings, and
     * documentation coverage; range 0–100.
     */
    private int qualityScore;

    /** Timestamp when this analysis was created. */
    private LocalDateTime createdAt;
}
