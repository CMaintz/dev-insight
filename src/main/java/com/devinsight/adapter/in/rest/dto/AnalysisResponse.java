package com.devinsight.adapter.in.rest.dto;

import com.devinsight.domain.model.AnalysisScope;

import java.time.LocalDateTime;
import java.util.UUID;

/**
 * REST response record for an {@link com.devinsight.domain.model.Analysis}.
 *
 * Returned by the analysis endpoints to provide the client with all
 * scored dimensions for a repository analysis run.
 *
 * @param repositoryId  internal UUID of the analysed repository
 * @param scope         the scope under which the analysis was run
 * @param overallScore  weighted aggregate score (0–100)
 * @param activityScore activity dimension score (0–100)
 * @param structureScore structure dimension score (0–100)
 * @param qualityScore  quality dimension score (0–100)
 * @param createdAt     timestamp when the analysis was created
 */
public record AnalysisResponse(
        UUID repositoryId,
        AnalysisScope scope,
        int overallScore,
        int activityScore,
        int structureScore,
        int qualityScore,
        LocalDateTime createdAt
) {
    /**
     * Convenience factory method to map from the domain {@link com.devinsight.domain.model.Analysis}.
     *
     * @param analysis the domain analysis object
     * @return a new {@link AnalysisResponse}
     */
    public static AnalysisResponse from(com.devinsight.domain.model.Analysis analysis) {
        return new AnalysisResponse(
                analysis.getRepositoryId(),
                analysis.getScope(),
                analysis.getOverallScore(),
                analysis.getActivityScore(),
                analysis.getStructureScore(),
                analysis.getQualityScore(),
                analysis.getCreatedAt()
        );
    }
}
