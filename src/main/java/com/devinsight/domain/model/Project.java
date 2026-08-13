package com.devinsight.domain.model;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.List;
import java.util.UUID;

/**
 * Core domain model representing a manually curated portfolio project.
 *
 * A Project is a higher-level concept than a single {@link Repository} — it may
 * aggregate multiple repositories (e.g. a frontend + backend monorepo split) and
 * allows the developer to add descriptive images and a custom name/description
 * for portfolio presentation purposes.
 */
@Data
@Builder
@AllArgsConstructor
@NoArgsConstructor
public class Project {

    /** Unique internal identifier for this project. */
    private UUID id;

    /** Foreign key to the owning {@link User}. */
    private UUID userId;

    /** Display name for this project on the portfolio. */
    private String name;

    /** Detailed description of the project's purpose, tech stack, and impact. */
    private String description;

    /**
     * List of image URLs (e.g. screenshots, architecture diagrams) to be
     * displayed in the portfolio carousel for this project.
     */
    private List<String> imageUrls;

    /**
     * UUIDs of {@link Repository} records that belong to this project.
     * Allows a project to span multiple GitHub repositories.
     */
    private List<UUID> linkedRepositoryIds;
}
