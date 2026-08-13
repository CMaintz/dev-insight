package com.devinsight.adapter.out.persistence.repository;

import com.devinsight.adapter.out.persistence.entity.FeedbackEntity;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.List;
import java.util.UUID;

/**
 * Spring Data JPA repository for {@link FeedbackEntity}.
 *
 * Provides standard CRUD operations inherited from {@link JpaRepository}
 * plus a custom finder for retrieving all feedback items for a given analysis.
 */
public interface FeedbackJpaRepository extends JpaRepository<FeedbackEntity, UUID> {

    /**
     * Returns all feedback entities associated with the given analysis.
     *
     * @param analysisId the internal UUID of the parent analysis
     * @return list of feedback entities; empty if none have been generated yet
     */
    List<FeedbackEntity> findByAnalysisId(UUID analysisId);
}
