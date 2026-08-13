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
 * JPA entity representing a row in the {@code feedback} table.
 *
 * Each row is a single feedback item generated for a completed analysis.
 * {@code type} and {@code severity} are stored as VARCHAR strings mapped
 * from {@link com.devinsight.domain.model.FeedbackType} and
 * {@link com.devinsight.domain.model.Severity} respectively.
 *
 * Mapping to and from the domain model is handled in
 * {@link com.devinsight.adapter.out.persistence.FeedbackPersistenceAdapter}.
 */
@Data
@Builder
@AllArgsConstructor
@NoArgsConstructor
@Entity
@Table(name = "feedback")
public class FeedbackEntity {

    /** Primary key — set in {@link #prePersist()} if not already assigned. */
    @Id
    @Column(name = "id", updatable = false, nullable = false)
    private UUID id;

    /** Foreign key referencing the parent analysis. */
    @Column(name = "analysis_id", nullable = false)
    private UUID analysisId;

    /**
     * String representation of {@link com.devinsight.domain.model.FeedbackType}.
     * Values: AI | RULE_BASED
     */
    @Column(name = "type", nullable = false)
    private String type;

    /** Human-readable feedback message. */
    @Column(name = "message", nullable = false, columnDefinition = "TEXT")
    private String message;

    /**
     * String representation of {@link com.devinsight.domain.model.Severity}.
     * Values: LOW | MEDIUM | HIGH
     */
    @Column(name = "severity", nullable = false)
    private String severity;

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
