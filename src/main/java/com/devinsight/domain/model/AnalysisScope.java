package com.devinsight.domain.model;

/**
 * Defines the scope of a repository analysis.
 *
 * REPO              - Analyses the entire repository as a standalone project.
 * USER_CONTRIBUTION - Analyses only commits and PRs authored by the user,
 *                     useful when the repository is shared/organisational.
 */
public enum AnalysisScope {
    REPO,
    USER_CONTRIBUTION
}
