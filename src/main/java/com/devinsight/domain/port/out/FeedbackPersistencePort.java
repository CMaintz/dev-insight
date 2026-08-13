package com.devinsight.domain.port.out;

import com.devinsight.domain.model.Feedback;

import java.util.List;
import java.util.UUID;

/**
 * Output port for persisting and retrieving {@link Feedback} domain objects.
 */
public interface FeedbackPersistencePort {

    /**
     * Persists a batch of feedback items in a single operation.
     *
     * Called after feedback generation to atomically store all items
     * produced for a given analysis.
     *
     * @param feedbacks the list of domain objects to persist
     * @return the list of persisted feedback items
     */
    List<Feedback> saveAll(List<Feedback> feedbacks);

    /**
     * Retrieves all feedback items associated with a given analysis.
     *
     * @param analysisId the internal UUID of the parent analysis
     * @return list of feedback items; empty list if none exist
     */
    List<Feedback> findByAnalysisId(UUID analysisId);
}
