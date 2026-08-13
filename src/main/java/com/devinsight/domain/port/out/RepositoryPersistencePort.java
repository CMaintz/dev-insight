package com.devinsight.domain.port.out;

import com.devinsight.domain.model.Repository;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

/**
 * Output port for persisting and retrieving {@link Repository} domain objects.
 *
 * Implementations adapt between the domain model and the underlying storage
 * mechanism (JPA, in-memory, etc.).
 */
public interface RepositoryPersistencePort {

    /**
     * Returns all repositories belonging to the given user.
     *
     * @param userId the internal UUID of the owning user
     * @return list of repositories; empty list if the user has no imported repos
     */
    List<Repository> findByUserId(UUID userId);

    /**
     * Persists a single repository and returns the saved state.
     *
     * @param repo the domain object to persist
     * @return the persisted repository
     */
    Repository save(Repository repo);

    /**
     * Retrieves a repository by its internal UUID.
     *
     * @param id the internal identifier
     * @return an {@link Optional} containing the repository, or empty if not found
     */
    Optional<Repository> findById(UUID id);

    /**
     * Persists a batch of repositories in a single operation.
     *
     * Used during the import flow to upsert all repositories returned by
     * the GitHub API in one round trip.
     *
     * @param repos the list of domain objects to persist
     * @return the list of persisted repositories
     */
    List<Repository> saveAll(List<Repository> repos);
}
