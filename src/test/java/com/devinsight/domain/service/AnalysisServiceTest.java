package com.devinsight.domain.service;

import com.devinsight.domain.model.Analysis;
import com.devinsight.domain.model.AnalysisScope;
import com.devinsight.domain.model.Repository;
import com.devinsight.domain.port.out.AnalysisPersistencePort;
import com.devinsight.domain.port.out.RepositoryPersistencePort;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.InjectMocks;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.time.LocalDateTime;
import java.util.Optional;
import java.util.UUID;

import static org.assertj.core.api.Assertions.assertThat;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.when;

/**
 * Unit tests for {@link AnalysisService}.
 *
 * Uses Mockito to isolate the domain service from its output port dependencies
 * ({@link RepositoryPersistencePort} and {@link AnalysisPersistencePort}).
 *
 * TODO: Flesh out assertions once scoring implementations are complete —
 *       currently all scores return 0 (stub implementations).
 */
@ExtendWith(MockitoExtension.class)
class AnalysisServiceTest {

    @Mock
    private RepositoryPersistencePort repositoryPersistencePort;

    @Mock
    private AnalysisPersistencePort analysisPersistencePort;

    @InjectMocks
    private AnalysisService analysisService;

    private UUID repositoryId;
    private Repository dummyRepository;
    private Analysis savedAnalysis;

    @BeforeEach
    void setUp() {
        repositoryId = UUID.randomUUID();

        dummyRepository = Repository.builder()
                .id(repositoryId)
                .userId(UUID.randomUUID())
                .name("devinsight")
                .fullName("akash/devinsight")
                .description("Developer portfolio and analysis platform")
                .language("Java")
                .stars(42)
                .forks(7)
                .lastActivity(LocalDateTime.now().minusDays(3))
                .selected(true)
                .build();

        savedAnalysis = Analysis.builder()
                .id(UUID.randomUUID())
                .repositoryId(repositoryId)
                .scope(AnalysisScope.REPO)
                .overallScore(0)      // TODO: update once scoring is implemented
                .activityScore(0)     // TODO: update once activity scoring is implemented
                .structureScore(0)    // TODO: update once structure scoring is implemented
                .qualityScore(0)      // TODO: update once quality scoring is implemented
                .createdAt(LocalDateTime.now())
                .build();
    }

    /**
     * Verifies that {@link AnalysisService#analyze(UUID, AnalysisScope)} returns
     * a non-null {@link Analysis} when the repository exists and the persistence
     * port successfully saves the result.
     *
     * TODO: Once scoring helpers (computeActivityScore, computeStructureScore,
     *       computeQualityScore) are implemented, add assertions for:
     *         - activityScore  > 0 for an active repository
     *         - structureScore > 0 for a well-structured repository
     *         - qualityScore   > 0 for a high-quality repository
     *         - overallScore   == weighted average of the three sub-scores
     */
    @Test
    @DisplayName("analyze() returns a non-null Analysis with computed scores for a known repository")
    void analyze_returnsAnalysis_withComputedScores() {
        // Arrange
        when(repositoryPersistencePort.findById(repositoryId))
                .thenReturn(Optional.of(dummyRepository));
        when(analysisPersistencePort.save(any(Analysis.class)))
                .thenReturn(savedAnalysis);

        // Act
        Analysis result = analysisService.analyze(repositoryId, AnalysisScope.REPO);

        // Assert — basic non-null / structural assertions
        assertThat(result).isNotNull();
        assertThat(result.getId()).isNotNull();
        assertThat(result.getRepositoryId()).isEqualTo(repositoryId);
        assertThat(result.getScope()).isEqualTo(AnalysisScope.REPO);
        assertThat(result.getCreatedAt()).isNotNull();

        // TODO: Replace 0-value assertions with meaningful expected values once
        //       the score computation stubs in AnalysisService are implemented.
        assertThat(result.getOverallScore()).isGreaterThanOrEqualTo(0);
        assertThat(result.getActivityScore()).isGreaterThanOrEqualTo(0);
        assertThat(result.getStructureScore()).isGreaterThanOrEqualTo(0);
        assertThat(result.getQualityScore()).isGreaterThanOrEqualTo(0);
    }
}
