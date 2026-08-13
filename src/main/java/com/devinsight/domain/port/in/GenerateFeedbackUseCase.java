package com.devinsight.domain.port.in;

import com.devinsight.domain.model.Feedback;

import java.util.List;
import java.util.UUID;

/**
 * Input port for generating actionable feedback for a completed analysis.
 *
 * Feedback is produced by applying rule-based thresholds and/or invoking an
 * AI service against the stored metric data for the given analysis.
 */
public interface GenerateFeedbackUseCase {

    /**
     * Generates and persists {@link Feedback} items for the specified analysis.
     *
     * This method is idempotent in the sense that existing feedback for the
     * same analysis should be replaced rather than duplicated.
     *
     * @param analysisId the internal {@link java.util.UUID} of the analysis
     * @return the list of newly generated and persisted {@link Feedback} items
     */
    List<Feedback> generateFeedback(UUID analysisId);
}
