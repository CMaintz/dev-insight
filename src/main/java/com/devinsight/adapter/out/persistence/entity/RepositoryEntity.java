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
 * JPA entity representing a row in the {@code repositories} table.
 *
 * Stores a snapshot of GitHub repository metadata imported by a DevInsight user.
 * Mapping to and from the domain model is handled in
 * {@link com.devinsight.adapter.out.persistence.RepositoryPersistenceAdapter}.
 */
@Data
@Builder
@AllArgsConstructor
@NoArgsConstructor
@Entity
@Table(name = "repositories")
public class RepositoryEntity {

    /** Primary key — set in {@link #prePersist()} if not already assigned. */
    @Id
    @Column(name = "id", updatable = false, nullable = false)
    private UUID id;

    /** Foreign key referencing the owning user. */
    @Column(name = "user_id", nullable = false)
    private UUID userId;

    /** Short repository name (e.g. "devinsight"). */
    @Column(name = "name", nullable = false)
    private String name;

    /** Full repository name including owner prefix (e.g. "akash/devinsight"). */
    @Column(name = "full_name", nullable = false)
    private String fullName;

    /** Repository description as set on GitHub. */
    @Column(name = "description", columnDefinition = "TEXT")
    private String description;

    /** Primary programming language detected by GitHub. */
    @Column(name = "language")
    private String language;

    /** Number of GitHub stars at the time of last import. */
    @Column(name = "stars", nullable = false)
    private int stars;

    /** Number of forks at the time of last import. */
    @Column(name = "forks", nullable = false)
    private int forks;

    /** Timestamp of the most recent push to the default branch. */
    @Column(name = "last_activity")
    private LocalDateTime lastActivity;

    /** Whether the user has chosen to feature this repo in their portfolio. */
    @Column(name = "selected", nullable = false)
    private boolean selected;

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
