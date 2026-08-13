package com.devinsight.adapter.out.persistence;

import com.devinsight.adapter.out.persistence.entity.FeedbackEntity;
import com.devinsight.adapter.out.persistence.repository.FeedbackJpaRepository;
import com.devinsight.domain.model.Feedback;
import com.devinsight.domain.model.FeedbackType;
import com.devinsight.domain.model.Severity;
import com.devinsight.domain.port.out.FeedbackPersistencePort;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Component;

import java.util.List;
import java.util.UUID;

/**
 * Secondary (driven) adapter implementing {@link FeedbackPersistencePort} using JPA.
 *
 * Handles mapping between the {@link Feedback} domain model and the
 * {@link FeedbackEntity} JPA entity.  Both {@link FeedbackType} and
 * {@link Severity} enums are stored as their name strings in the database.
 *
 * TODO: use MapStruct here — generate a FeedbackMapper annotated with
 *       {@code @Mapper(componentModel = "spring")} and inject it via constructor.
 */
@Component
@RequiredArgsConstructor
public class FeedbackPersistenceAdapter implements FeedbackPersistencePort {

    private final FeedbackJpaRepository feedbackJpaRepository;

    @Override
    public List<Feedback> saveAll(List<Feedback> feedbacks) {
        List<FeedbackEntity> entities = feedbacks.stream()
                .map(this::toEntity)
                .toList();
        return feedbackJpaRepository.saveAll(entities).stream()
                .map(this::toDomain)
                .toList();
    }

    @Override
    public List<Feedback> findByAnalysisId(UUID analysisId) {
        return feedbackJpaRepository.findByAnalysisId(analysisId).stream()
                .map(this::toDomain)
                .toList();
    }

    // -------------------------------------------------------------------------
    // Mapping helpers — TODO: replace with MapStruct
    // -------------------------------------------------------------------------

    /**
     * Maps a {@link FeedbackEntity} to a {@link Feedback} domain object.
     *
     * TODO: use MapStruct here
     */
    private Feedback toDomain(FeedbackEntity entity) {
        return Feedback.builder()
                .id(entity.getId())
                .analysisId(entity.getAnalysisId())
                .type(FeedbackType.valueOf(entity.getType()))
                .message(entity.getMessage())
                .severity(Severity.valueOf(entity.getSeverity()))
                .build();
    }

    /**
     * Maps a {@link Feedback} domain object to a {@link FeedbackEntity}.
     *
     * TODO: use MapStruct here
     */
    private FeedbackEntity toEntity(Feedback feedback) {
        return FeedbackEntity.builder()
                .id(feedback.getId())
                .analysisId(feedback.getAnalysisId())
                .type(feedback.getType().name())
                .message(feedback.getMessage())
                .severity(feedback.getSeverity().name())
                .build();
    }
}
