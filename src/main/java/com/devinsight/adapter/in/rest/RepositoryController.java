package com.devinsight.adapter.in.rest;

import com.devinsight.domain.model.Repository;
import com.devinsight.domain.port.in.ImportRepositoriesUseCase;
import com.devinsight.domain.port.in.SelectRepositoryUseCase;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PatchMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.util.List;
import java.util.UUID;

/**
 * REST adapter for repository management endpoints.
 *
 * Exposes operations for importing GitHub repositories and toggling their
 * portfolio selection state.
 *
 * TODO: Extract the authenticated userId from the JWT security context
 *       (via a custom Principal or SecurityContextHolder) rather than
 *       accepting it as a request parameter.
 */
@Slf4j
@RestController
@RequestMapping("/repos")
@RequiredArgsConstructor
public class RepositoryController {

    private final ImportRepositoriesUseCase importRepositoriesUseCase;
    private final SelectRepositoryUseCase selectRepositoryUseCase;

    /**
     * Imports all GitHub repositories for the currently authenticated user.
     *
     * GET /repos?userId={userId}
     *
     * TODO: Resolve userId from the JWT Principal instead of a query parameter.
     *
     * @param userId the internal UUID of the authenticated user (temporary — see TODO)
     * @return 200 OK with the list of imported {@link Repository} objects
     */
    @GetMapping
    public ResponseEntity<List<Repository>> importRepositories(
            @RequestParam("userId") UUID userId) {

        log.info("Import repositories request for userId={}", userId);
        List<Repository> repos = importRepositoriesUseCase.importRepositories(userId);
        return ResponseEntity.ok(repos);
    }

    /**
     * Toggles whether a repository is featured in the user's public portfolio.
     *
     * PATCH /repos/{id}/select?selected=true
     *
     * @param id       the internal UUID of the repository to update
     * @param selected {@code true} to feature the repo; {@code false} to hide it
     * @return 200 OK with the updated {@link Repository}
     */
    @PatchMapping("/{id}/select")
    public ResponseEntity<Repository> selectRepository(
            @PathVariable("id") UUID id,
            @RequestParam("selected") boolean selected) {

        log.info("Set selected={} for repositoryId={}", selected, id);
        Repository updated = selectRepositoryUseCase.setSelected(id, selected);
        return ResponseEntity.ok(updated);
    }
}
