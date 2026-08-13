package com.devinsight.adapter.out.persistence.entity;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import jakarta.persistence.PrePersist;
import jakarta.persistence.Table;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.UUID;

/**
 * JPA entity representing a row in the {@code analysis_metrics} table.
 *
 * Each row corresponds to a single named numeric metric collected during
 * an analysis run.  Metrics are the raw inputs from which dimension scores
 * are computed.
 *
 * Mapping to and from the domain model is handled in
 * {@link com.devinsight.adapter.out.persistence.AnalysisPersistenceAdapter}.
 */
@Data
@Builder
@AllArgsConstructor
@NoArgsConstructor
@Entity
@Table(name = "analysis_metrics")
public class AnalysisMetricEntity {

    /** Primary key — set in {@link #prePersist()} if not already assigned. */
    @Id
    @Column(name = "id", updatable = false, nullable = false)
    private UUID id;

    /** Foreign key referencing the parent analysis. */
    @Column(name = "analysis_id", nullable = false)
    private UUID analysisId;

    /** Human-readable metric key (snake_case convention). */
    @Column(name = "name", nullable = false)
    private String name;

    /** Numeric value of the metric. */
    @Column(name = "value", nullable = false)
    private double value;

    /** Whether this metric is fed into a score calculation. */
    @Column(name = "included_in_score", nullable = false)
    private boolean includedInScore;

    /**
     * Ensures that {@code id} is populated before the entity is first written
     * to the database.
     */
    @PrePersist
    void prePersist() {
        if (id == null) {
            id = UUID.randomUUID();
        }
    }
}
