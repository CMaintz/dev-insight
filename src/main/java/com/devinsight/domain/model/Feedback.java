package com.devinsight.domain.model;

import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

import java.util.UUID;

/**
 * A piece of actionable feedback attached to an {@link Analysis}.
 *
 * Feedback is generated either by deterministic rules applied to metric
 * thresholds ({@link FeedbackType#RULE_BASED}) or by an AI/LLM service
 * ({@link FeedbackType#AI}).  Each item is assigned a {@link Severity}
 * that indicates how urgently the developer should act on it.
 */
@Data
@Builder
@AllArgsConstructor
@NoArgsConstructor
public class Feedback {

    /** Unique internal identifier for this feedback item. */
    private UUID id;

    /** Foreign key to the parent {@link Analysis}. */
    private UUID analysisId;

    /** Origin of this feedback — AI-generated or rule-based. */
    private FeedbackType type;

    /** Human-readable feedback message to be displayed to the developer. */
    private String message;

    /** Severity level indicating the priority of addressing this feedback. */
    private Severity severity;
}
