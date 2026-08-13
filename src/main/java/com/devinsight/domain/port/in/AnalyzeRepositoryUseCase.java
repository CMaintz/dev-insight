package com.devinsight.domain.port.in;

import com.devinsight.domain.model.Analysis;
import com.devinsight.domain.model.AnalysisScope;

import java.util.UUID;

/**
 * Input port for triggering a scored analysis of a repository.
 *
 * Computes activity, structure, and quality scores by querying the GitHub API
 * (via an output port) and persists the resulting {@link Analysis} record.
 */
public interface AnalyzeRepositoryUseCase {

    /**
     * Runs a full analysis on the specified repository under the given scope
     * and returns the persisted {@link Analysis} result.
     *
     * @param repositoryId the internal {@link java.util.UUID} of the repository to analyse
     * @param scope        the {@link AnalysisScope} controlling which data is considered
     * @return the newly created and persisted {@link Analysis} domain object
     */
    Analysis analyze(UUID repositoryId, AnalysisScope scope);
}
