package com.devinsight.domain.service;

import com.devinsight.domain.model.Analysis;
import com.devinsight.domain.model.Feedback;
import com.devinsight.domain.model.FeedbackType;
import com.devinsight.domain.model.Severity;
import com.devinsight.domain.port.in.GenerateFeedbackUseCase;
import com.devinsight.domain.port.out.AnalysisPersistencePort;
import com.devinsight.domain.port.out.FeedbackPersistencePort;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

import java.util.ArrayList;
import java.util.List;
import java.util.UUID;

/**
 * Domain service implementing the {@link GenerateFeedbackUseCase}.
 *
 * Currently applies a set of rule-based threshold checks against the scores
 * stored on the {@link Analysis}.  AI-generated feedback (calling an LLM)
 * is a planned enhancement — see the TODO comments below.
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class FeedbackService implements GenerateFeedbackUseCase {

    private final AnalysisPersistencePort analysisPersistencePort;
    private final FeedbackPersistencePort feedbackPersistencePort;

    /**
     * Generates and persists feedback items for the given analysis.
     *
     * Flow:
     * 1. Resolve the {@link Analysis} — throws if not found.
     * 2. Apply rule-based threshold checks to each score dimension.
     * 3. TODO: Invoke AI service for additional narrative feedback.
     * 4. Persist all generated feedback and return the saved list.
     *
     * @param analysisId the internal UUID of the analysis to generate feedback for
     * @return the persisted list of {@link Feedback} items
     */
    @Override
    public List<Feedback> generateFeedback(UUID analysisId) {
        log.info("Generating feedback for analysisId={}", analysisId);

        Analysis analysis = analysisPersistencePort.findByRepositoryId(
                        // Workaround: findByRepositoryId scoped to this analysisId is not ideal;
                        // TODO: add AnalysisPersistencePort.findById(UUID) output port method
                        UUID.fromString("00000000-0000-0000-0000-000000000000"))
                .stream()
                .filter(a -> a.getId().equals(analysisId))
                .findFirst()
                .orElseThrow(() -> new IllegalArgumentException(
                        "Analysis not found for analysisId=" + analysisId));

        // TODO: add AnalysisPersistencePort.findById(UUID id) to avoid the above workaround.

        List<Feedback> feedbacks = new ArrayList<>();
        feedbacks.addAll(applyRuleBasedFeedback(analysis));

        // TODO: Call AI service (e.g. OpenAI Chat Completions API) with the analysis
        //       scores and repository metadata to generate narrative feedback items.
        //       Add them to the feedbacks list with type = FeedbackType.AI.

        List<Feedback> saved = feedbackPersistencePort.saveAll(feedbacks);
        log.info("Generated {} feedback items for analysisId={}", saved.size(), analysisId);

        return saved;
    }

    /**
     * Applies deterministic threshold rules to the analysis scores and
     * produces a list of rule-based feedback items.
     *
     * TODO: Expand these rules and externalise thresholds to application.yml.
     *
     * @param analysis the resolved analysis containing scores
     * @return list of rule-based {@link Feedback} items (may be empty)
     */
    private List<Feedback> applyRuleBasedFeedback(Analysis analysis) {
        List<Feedback> feedbacks = new ArrayList<>();

        // Rule 1: Low activity score
        if (analysis.getActivityScore() < 40) {
            feedbacks.add(Feedback.builder()
                    .analysisId(analysis.getId())
                    .type(FeedbackType.RULE_BASED)
                    .severity(analysis.getActivityScore() < 20 ? Severity.HIGH : Severity.MEDIUM)
                    .message("Repository activity is low. Consider increasing commit frequency " +
                             "and engaging with issues and pull requests more regularly.")
                    .build());
        }

        // Rule 2: Low structure score
        if (analysis.getStructureScore() < 40) {
            feedbacks.add(Feedback.builder()
                    .analysisId(analysis.getId())
                    .type(FeedbackType.RULE_BASED)
                    .severity(Severity.MEDIUM)
                    .message("Repository structure could be improved. Ensure a README, " +
                             "CONTRIBUTING guide, LICENSE file, and CI configuration are present.")
                    .build());
        }

        // Rule 3: Low quality score
        if (analysis.getQualityScore() < 40) {
            feedbacks.add(Feedback.builder()
                    .analysisId(analysis.getId())
                    .type(FeedbackType.RULE_BASED)
                    .severity(analysis.getQualityScore() < 20 ? Severity.HIGH : Severity.MEDIUM)
                    .message("Code quality metrics are below the recommended threshold. " +
                             "Consider adding tests, reducing cyclomatic complexity, and " +
                             "improving documentation coverage.")
                    .build());
        }

        // TODO: Add more granular rules as additional metrics become available.

        return feedbacks;
    }
}
