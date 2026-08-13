package com.devinsight.adapter.out.persistence.repository;

import com.devinsight.adapter.out.persistence.entity.RepositoryEntity;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.UUID;

/**
 * Spring Data JPA repository for {@link RepositoryEntity}.
 *
 * Provides standard CRUD operations inherited from {@link JpaRepository}
 * plus a custom finder for loading all repositories owned by a given user.
 */
public interface RepositoryJpaRepository extends JpaRepository<RepositoryEntity, UUID> {

    /**
     * Returns all repository entities belonging to the given user.
     *
     * Used when populating the dashboard and when importing repositories
     * to display the current state.
     *
     * @param userId the internal UUID of the owning user
     * @return list of repository entities; empty if the user has imported none
     */
    List<RepositoryEntity> findByUserId(UUID userId);
}
