package com.devinsight.domain.model;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.time.LocalDateTime;
import java.util.UUID;

/**
 * Core domain model representing a DevInsight user.
 *
 * A user authenticates via GitHub OAuth and may optionally enrich their
 * profile with a LinkedIn URL and a short bio for their public portfolio.
 */
@Data
@Builder
@AllArgsConstructor
@NoArgsConstructor
public class User {

    /** Unique internal identifier for the user. */
    private UUID id;

    /** Primary email address sourced from GitHub (may be null if private). */
    private String email;

    /** GitHub node ID — used as the stable external identity key. */
    private String githubId;

    /** GitHub login/username displayed on the portfolio. */
    private String githubUsername;

    /** Optional LinkedIn profile URL for the public portfolio page. */
    private String linkedinUrl;

    /** Short biography shown on the portfolio. */
    private String bio;

    /** Timestamp when the user record was first created. */
    private LocalDateTime createdAt;
}
