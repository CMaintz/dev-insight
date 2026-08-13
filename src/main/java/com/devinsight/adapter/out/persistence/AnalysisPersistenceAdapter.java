package com.devinsight.adapter.out.persistence;

import com.devinsight.adapter.out.persistence.entity.AnalysisEntity;
import com.devinsight.adapter.out.persistence.repository.AnalysisJpaRepository;
import com.devinsight.domain.model.Analysis;
import com.devinsight.domain.model.AnalysisScope;
import com.devinsight.domain.port.out.AnalysisPersistencePort;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Component;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

/**
 * Secondary (driven) adapter implementing {@link AnalysisPersistencePort} using JPA.
 *
 * Handles mapping between the {@link Analysis} domain model and the
 * {@link AnalysisEntity} JPA entity.  The {@link AnalysisScope} enum is stored
 * as its name string in the database.
 *
 * TODO: use MapStruct here — generate an AnalysisMapper annotated with
 *       {@code @Mapper(componentModel = "spring")} and inject it via constructor.
 */
@Component
@RequiredArgsConstructor
public class AnalysisPersistenceAdapter implements AnalysisPersistencePort {

    private final AnalysisJpaRepository analysisJpaRepository;

    @Override
    public Analysis save(Analysis analysis) {
        AnalysisEntity entity = toEntity(analysis);
        AnalysisEntity saved = analysisJpaRepository.save(entity);
        return toDomain(saved);
    }

    @Override
    public Optional<Analysis> findByRepositoryIdAndScope(UUID repositoryId, AnalysisScope scope) {
        return analysisJpaRepository
                .findByRepositoryIdAndScope(repositoryId, scope.name())
                .map(this::toDomain);
    }

    @Override
    public List<Analysis> findByRepositoryId(UUID repositoryId) {
        return analysisJpaRepository.findByRepositoryId(repositoryId).stream()
                .map(this::toDomain)
                .toList();
    }

    // -------------------------------------------------------------------------
    // Mapping helpers — TODO: replace with MapStruct
    // -------------------------------------------------------------------------

    /**
     * Maps an {@link AnalysisEntity} to an {@link Analysis} domain object.
     *
     * TODO: use MapStruct here
     */
    private Analysis toDomain(AnalysisEntity entity) {
        return Analysis.builder()
                .id(entity.getId())
                .repositoryId(entity.getRepositoryId())
                .scope(AnalysisScope.valueOf(entity.getScope()))
                .overallScore(entity.getOverallScore())
                .activityScore(entity.getActivityScore())
                .structureScore(entity.getStructureScore())
                .qualityScore(entity.getQualityScore())
                .createdAt(entity.getCreatedAt())
                .build();
    }

    /**
     * Maps an {@link Analysis} domain object to an {@link AnalysisEntity}.
     *
     * TODO: use MapStruct here
     */
    private AnalysisEntity toEntity(Analysis analysis) {
        return AnalysisEntity.builder()
                .id(analysis.getId())
                .repositoryId(analysis.getRepositoryId())
                .scope(analysis.getScope().name())
                .overallScore(analysis.getOverallScore())
                .activityScore(analysis.getActivityScore())
                .structureScore(analysis.getStructureScore())
                .qualityScore(analysis.getQualityScore())
                .createdAt(analysis.getCreatedAt())
                .build();
    }
}
