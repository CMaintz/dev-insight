package com.devinsight.adapter.in.rest;

import com.devinsight.domain.model.Feedback;
import com.devinsight.domain.port.in.GenerateFeedbackUseCase;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;
import java.util.UUID;

/**
 * REST adapter for feedback retrieval endpoints.
 *
 * Feedback is generated automatically when an analysis is run (see
 * {@link AnalysisController#runAnalysis}). This controller allows the
 * client to retrieve the generated feedback items for a given analysis.
 */
@Slf4j
@RestController
@RequestMapping("/feedback")
@RequiredArgsConstructor
public class FeedbackController {

    private final GenerateFeedbackUseCase generateFeedbackUseCase;

    /**
     * Retrieves all feedback items associated with the given analysis.
     *
     * GET /feedback/{analysisId}
     *
     * TODO: Add a dedicated GetFeedbackUseCase that reads from persistence
     *       rather than regenerating feedback on every GET request.
     *       Currently this re-generates feedback, which is fine for a stub
     *       but should be replaced with a read-only query.
     *
     * @param analysisId the internal UUID of the analysis
     * @return 200 OK with the list of {@link Feedback} items
     */
    @GetMapping("/{analysisId}")
    public ResponseEntity<List<Feedback>> getFeedback(
            @PathVariable("analysisId") UUID analysisId) {

        log.info("Fetching feedback for analysisId={}", analysisId);

        // TODO: Replace with a read-only GetFeedbackUseCase.findByAnalysisId(analysisId).
        List<Feedback> feedbacks = generateFeedbackUseCase.generateFeedback(analysisId);

        return ResponseEntity.ok(feedbacks);
    }
}
