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

import java.time.LocalDateTime;
import java.util.UUID;

/**
 * JPA entity representing a row in the {@code analyses} table.
 *
 * Stores the computed scores for a single analysis run against a repository.
 * The {@code scope} field maps to the {@link com.devinsight.domain.model.AnalysisScope}
 * enum and is stored as a VARCHAR for portability.
 *
 * Mapping to and from the domain model is handled in
 * {@link com.devinsight.adapter.out.persistence.AnalysisPersistenceAdapter}.
 */
@Data
@Builder
@AllArgsConstructor
@NoArgsConstructor
@Entity
@Table(name = "analyses")
public class AnalysisEntity {

    /** Primary key — set in {@link #prePersist()} if not already assigned. */
    @Id
    @Column(name = "id", updatable = false, nullable = false)
    private UUID id;

    /** Foreign key referencing the analysed repository. */
    @Column(name = "repository_id", nullable = false)
    private UUID repositoryId;

    /**
     * String representation of {@link com.devinsight.domain.model.AnalysisScope}.
     * Stored as VARCHAR rather than an enum column for schema flexibility.
     */
    @Column(name = "scope", nullable = false)
    private String scope;

    /** Weighted aggregate score (0–100). */
    @Column(name = "overall_score", nullable = false)
    private int overallScore;

    /** Activity dimension score (0–100). */
    @Column(name = "activity_score", nullable = false)
    private int activityScore;

    /** Structure dimension score (0–100). */
    @Column(name = "structure_score", nullable = false)
    private int structureScore;

    /** Quality dimension score (0–100). */
    @Column(name = "quality_score", nullable = false)
    private int qualityScore;

    /** Timestamp when the analysis was created — set in {@link #prePersist()}. */
    @Column(name = "created_at", updatable = false, nullable = false)
    private LocalDateTime createdAt;

    /**
     * Ensures that {@code id} and {@code createdAt} are populated before the
     * entity is first written to the database.
     */
    @PrePersist
    void prePersist() {
        if (id == null) {
            id = UUID.randomUUID();
        }
        if (createdAt == null) {
            createdAt = LocalDateTime.now();
        }
    }
}
