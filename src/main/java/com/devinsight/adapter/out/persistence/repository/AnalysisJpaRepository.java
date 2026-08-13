package com.devinsight.adapter.out.persistence.repository;

import com.devinsight.adapter.out.persistence.entity.AnalysisEntity;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

/**
 * Spring Data JPA repository for {@link AnalysisEntity}.
 *
 * Provides standard CRUD operations inherited from {@link JpaRepository}
 * plus custom finders used by the analysis persistence adapter.
 *
 * Note: {@code scope} is stored as a String in the database (matching the
 * {@link com.devinsight.domain.model.AnalysisScope} enum name).
 */
public interface AnalysisJpaRepository extends JpaRepository<AnalysisEntity, UUID> {

    /**
     * Finds the most recent analysis for a specific repository/scope combination.
     *
     * Because scope is stored as a VARCHAR, the query parameter must be the
     * {@link com.devinsight.domain.model.AnalysisScope} name (e.g. {@code "REPO"}).
     *
     * @param repositoryId the internal UUID of the analysed repository
     * @param scope        the scope string (enum name)
     * @return an {@link Optional} containing the matching analysis entity, or empty
     */
    Optional<AnalysisEntity> findByRepositoryIdAndScope(UUID repositoryId, String scope);

    /**
     * Returns all analysis entities for a given repository, typically ordered
     * by creation time to allow the caller to select the most recent.
     *
     * @param repositoryId the internal UUID of the repository
     * @return list of analysis entities; empty if no analyses have been run yet
     */
    List<AnalysisEntity> findByRepositoryId(UUID repositoryId);
}
