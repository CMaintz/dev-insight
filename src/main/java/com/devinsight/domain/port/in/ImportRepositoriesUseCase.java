package com.devinsight.domain.port.in;

import com.devinsight.domain.model.Repository;

import java.util.List;
import java.util.UUID;

/**
 * Input port for importing a user's GitHub repositories into DevInsight.
 *
 * Triggers a fresh fetch from the GitHub API and persists/updates the
 * resulting repository records so they are available for analysis and
 * portfolio curation.
 */
public interface ImportRepositoriesUseCase {

    /**
     * Fetches all public (and, where authorised, private) repositories for
     * the given user from GitHub and upserts them into the local store.
     *
     * @param userId the internal {@link java.util.UUID} of the owning user
     * @return the full list of persisted {@link Repository} domain objects
     */
    List<Repository> importRepositories(UUID userId);
}
