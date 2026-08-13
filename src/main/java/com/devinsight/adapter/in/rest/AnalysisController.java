package com.devinsight.adapter.in.rest;

import com.devinsight.adapter.in.rest.dto.AnalysisResponse;
import com.devinsight.domain.model.AnalysisScope;
import com.devinsight.domain.port.in.AnalyzeRepositoryUseCase;
import com.devinsight.domain.port.in.GenerateFeedbackUseCase;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.util.UUID;

/**
 * REST adapter for repository analysis endpoints.
 *
 * Allows authenticated users to trigger new analysis runs and retrieve
 * existing analysis results for a given repository.
 */
@Slf4j
@RestController
@RequestMapping("/analysis")
@RequiredArgsConstructor
public class AnalysisController {

    private final AnalyzeRepositoryUseCase analyzeRepositoryUseCase;
    private final GenerateFeedbackUseCase generateFeedbackUseCase;

    /**
     * Triggers a new analysis run for the specified repository.
     *
     * After the analysis is complete, feedback is generated automatically.
     *
     * POST /analysis/run/{repoId}?scope=REPO
     *
     * @param repoId the internal UUID of the repository to analyse
     * @param scope  the {@link AnalysisScope} to use; defaults to {@code REPO}
     * @return 200 OK with the {@link AnalysisResponse} containing all scores
     */
    @PostMapping("/run/{repoId}")
    public ResponseEntity<AnalysisResponse> runAnalysis(
            @PathVariable("repoId") UUID repoId,
            @RequestParam(value = "scope", defaultValue = "REPO") AnalysisScope scope) {

        log.info("Running analysis for repositoryId={}, scope={}", repoId, scope);

        var analysis = analyzeRepositoryUseCase.analyze(repoId, scope);

        // Eagerly generate feedback so the client can immediately query /feedback/{analysisId}
        // TODO: Consider making feedback generation asynchronous via an application event.
        generateFeedbackUseCase.generateFeedback(analysis.getId());

        return ResponseEntity.ok(AnalysisResponse.from(analysis));
    }

    /**
     * Retrieves the most recent analysis result for the specified repository.
     *
     * GET /analysis/{repoId}?scope=REPO
     *
     * TODO: Delegate to a dedicated GetAnalysisUseCase rather than re-running the analysis.
     *       This endpoint should read from the persistence layer, not trigger computation.
     *
     * @param repoId the internal UUID of the repository
     * @param scope  the {@link AnalysisScope} to filter by; defaults to {@code REPO}
     * @return 200 OK with the latest {@link AnalysisResponse} for the given scope
     */
    @GetMapping("/{repoId}")
    public ResponseEntity<AnalysisResponse> getAnalysis(
            @PathVariable("repoId") UUID repoId,
            @RequestParam(value = "scope", defaultValue = "REPO") AnalysisScope scope) {

        log.info("Fetching analysis for repositoryId={}, scope={}", repoId, scope);

        // TODO: Replace with a GetAnalysisUseCase.findByRepositoryIdAndScope(repoId, scope)
        //       that reads persisted results rather than running a new analysis.
        var analysis = analyzeRepositoryUseCase.analyze(repoId, scope);

        return ResponseEntity.ok(AnalysisResponse.from(analysis));
    }
}
