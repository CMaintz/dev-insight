package com.devinsight.adapter.out.persistence.repository;

import com.devinsight.adapter.out.persistence.entity.UserEntity;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;
import java.util.UUID;

/**
 * Spring Data JPA repository for {@link UserEntity}.
 *
 * Provides standard CRUD operations inherited from {@link JpaRepository}
 * plus a custom finder for looking up users by their stable GitHub account ID.
 */
public interface UserJpaRepository extends JpaRepository<UserEntity, UUID> {

    /**
     * Finds a user by their stable GitHub account/node ID.
     *
     * Used during the login flow to determine whether an authenticating
     * GitHub user already has a DevInsight account.
     *
     * @param githubId the stable GitHub account ID (not the login username)
     * @return an {@link Optional} containing the matching entity, or empty
     */
    Optional<UserEntity> findByGithubId(String githubId);
}
