package com.devinsight.domain.port.out;

import com.devinsight.domain.model.Analysis;
import com.devinsight.domain.model.AnalysisScope;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

/**
 * Output port for persisting and retrieving {@link Analysis} domain objects.
 */
public interface AnalysisPersistencePort {

    /**
     * Persists a new or updated analysis and returns the saved state.
     *
     * @param analysis the domain object to persist
     * @return the persisted analysis
     */
    Analysis save(Analysis analysis);

    /**
     * Retrieves the most recent analysis for a given repository and scope combination.
     *
     * There should be at most one canonical analysis per (repositoryId, scope) pair
     * at any point in time; older runs may be archived or replaced.
     *
     * @param repositoryId the internal UUID of the analysed repository
     * @param scope        the analysis scope
     * @return an {@link Optional} containing the matching analysis, or empty
     */
    Optional<Analysis> findByRepositoryIdAndScope(UUID repositoryId, AnalysisScope scope);

    /**
     * Returns all analyses ever run for the given repository, ordered by
     * creation time descending.
     *
     * @param repositoryId the internal UUID of the repository
     * @return list of analyses; empty list if none have been run yet
     */
    List<Analysis> findByRepositoryId(UUID repositoryId);
}
