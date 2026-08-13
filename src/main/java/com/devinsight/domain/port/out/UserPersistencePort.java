package com.devinsight.domain.port.out;

import com.devinsight.domain.model.User;

import java.util.Optional;
import java.util.UUID;

/**
 * Output port for persisting and retrieving {@link User} domain objects.
 *
 * Implementations map between the domain model and whatever storage technology
 * is in use (JPA entity, document, etc.) without exposing that detail to the domain.
 */
public interface UserPersistencePort {

    /**
     * Looks up a user by their stable GitHub account ID.
     *
     * Used during login to determine whether a GitHub user already has a
     * DevInsight account or needs to be created.
     *
     * @param githubId the stable GitHub node/account ID
     * @return an {@link Optional} containing the matching user, or empty if not found
     */
    Optional<User> findByGithubId(String githubId);

    /**
     * Persists a new or updated {@link User} and returns the saved state.
     *
     * @param user the domain object to persist
     * @return the persisted user (may include server-generated fields such as id/createdAt)
     */
    User save(User user);

    /**
     * Retrieves a user by internal UUID.
     *
     * @param id the internal identifier
     * @return an {@link Optional} containing the user, or empty if not found
     */
    Optional<User> findById(UUID id);
}
