package com.devinsight.domain.service;

import com.devinsight.domain.model.Repository;
import com.devinsight.domain.model.User;
import com.devinsight.domain.port.in.ImportRepositoriesUseCase;
import com.devinsight.domain.port.out.GitHubPort;
import com.devinsight.domain.port.out.RepositoryPersistencePort;
import com.devinsight.domain.port.out.UserPersistencePort;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

import java.util.List;
import java.util.UUID;

/**
 * Domain service implementing the {@link ImportRepositoriesUseCase}.
 *
 * Fetches all repositories for the authenticated user from GitHub and
 * upserts them into the local persistence store.
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class ImportRepositoriesService implements ImportRepositoriesUseCase {

    private final GitHubPort gitHubPort;
    private final RepositoryPersistencePort repositoryPersistencePort;
    private final UserPersistencePort userPersistencePort;

    /**
     * Imports all GitHub repositories for the given user.
     *
     * Flow:
     * 1. Resolve the {@link User} by internal UUID — throws if not found.
     * 2. Use the user's GitHub username to fetch repositories via the GitHub API.
     * 3. Attach the internal userId to each repository domain object.
     * 4. Batch-save the repositories and return the persisted list.
     *
     * @param userId the internal UUID of the user whose repos should be imported
     * @return the saved list of {@link Repository} domain objects
     * @throws IllegalArgumentException if no user exists with the given id
     */
    @Override
    public List<Repository> importRepositories(UUID userId) {
        log.info("Importing repositories for userId={}", userId);

        User user = userPersistencePort.findById(userId)
                .orElseThrow(() -> new IllegalArgumentException(
                        "User not found for userId=" + userId));

        log.debug("Fetching GitHub repositories for githubUsername={}", user.getGithubUsername());

        List<Repository> fetched = gitHubPort.fetchRepositories(user.getGithubUsername());

        // Attach the internal userId to every fetched repository
        List<Repository> withUserId = fetched.stream()
                .map(repo -> Repository.builder()
                        .id(repo.getId())
                        .userId(userId)
                        .name(repo.getName())
                        .fullName(repo.getFullName())
                        .description(repo.getDescription())
                        .language(repo.getLanguage())
                        .stars(repo.getStars())
                        .forks(repo.getForks())
                        .lastActivity(repo.getLastActivity())
                        .selected(repo.isSelected())
                        .build())
                .toList();

        List<Repository> saved = repositoryPersistencePort.saveAll(withUserId);
        log.info("Imported {} repositories for userId={}", saved.size(), userId);

        return saved;
    }
}
