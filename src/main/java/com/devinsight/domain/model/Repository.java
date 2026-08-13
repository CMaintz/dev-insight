package com.devinsight.domain.model;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;
import java.util.UUID;

/**
 * Core domain model representing a GitHub repository imported by a user.
 *
 * Repositories are fetched from the GitHub API and stored locally so that
 * analysis results and portfolio selections can be persisted independently
 * of GitHub rate limits.
 */
@Data
@Builder
@AllArgsConstructor
@NoArgsConstructor
public class Repository {

    /** Unique internal identifier for this repository record. */
    private UUID id;

    /** Foreign key to the owning {@link User}. */
    private UUID userId;

    /** Short repository name (e.g. "devinsight"). */
    private String name;

    /** Full repository name including owner (e.g. "akash/devinsight"). */
    private String fullName;

    /** Repository description as set on GitHub. */
    private String description;

    /** Primary programming language detected by GitHub. */
    private String language;

    /** Number of GitHub stars at the time of last import. */
    private int stars;

    /** Number of forks at the time of last import. */
    private int forks;

    /** Timestamp of the most recent push/commit to the default branch. */
    private LocalDateTime lastActivity;

    /**
     * Whether the user has chosen to feature this repository in their portfolio.
     * Only selected repositories appear in the public portfolio view.
     */
    private boolean selected;
}
