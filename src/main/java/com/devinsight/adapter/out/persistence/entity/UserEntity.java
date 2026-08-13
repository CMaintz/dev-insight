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
 * JPA entity representing a row in the {@code users} table.
 *
 * This is a persistence-layer concern and should not leak into the domain.
 * Mapping between this entity and the {@link com.devinsight.domain.model.User}
 * domain model is handled in {@link com.devinsight.adapter.out.persistence.UserPersistenceAdapter}.
 */
@Data
@Builder
@AllArgsConstructor
@NoArgsConstructor
@Entity
@Table(name = "users")
public class UserEntity {

    /** Primary key — set in {@link #prePersist()} if not already assigned. */
    @Id
    @Column(name = "id", updatable = false, nullable = false)
    private UUID id;

    /** User's primary email address (may be null if private on GitHub). */
    @Column(name = "email")
    private String email;

    /** Stable GitHub account/node ID used as the external identity key. */
    @Column(name = "github_id", unique = true, nullable = false)
    private String githubId;

    /** GitHub login username (may change over time). */
    @Column(name = "github_username", nullable = false)
    private String githubUsername;

    /** Optional LinkedIn profile URL. */
    @Column(name = "linkedin_url")
    private String linkedinUrl;

    /** Short biography for the public portfolio. */
    @Column(name = "bio", columnDefinition = "TEXT")
    private String bio;

    /** Timestamp when the record was first created — set in {@link #prePersist()}. */
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
