package com.devinsight.adapter.out.github;

import com.devinsight.domain.model.Repository;
import com.devinsight.domain.port.out.GitHubPort;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import java.util.List;

/**
 * Secondary (driven) adapter implementing {@link GitHubPort} using the
 * org.kohsuke:github-api library.
 *
 * This adapter is responsible for all communication with the GitHub REST API.
 * It converts raw GitHub API objects into domain model objects so that the
 * domain layer remains decoupled from the GitHub client library.
 *
 * TODO: Wire in org.kohsuke.github.GitHub client — either via
 *       GitHub.connectUsingOAuth(accessToken) for per-request clients, or
 *       by creating a shared app installation client for repository metadata.
 */
@Slf4j
@Component
public class GitHubAdapter implements GitHubPort {

    /**
     * GitHub OAuth application client ID, injected from application configuration.
     * Used when exchanging OAuth codes for access tokens.
     */
    @Value("${github.client-id}")
    private String clientId;

    /**
     * GitHub OAuth application client secret, injected from application configuration.
     * Used when exchanging OAuth codes for access tokens.
     */
    @Value("${github.client-secret}")
    private String clientSecret;

    /**
     * Fetches all public repositories for the given GitHub username.
     *
     * TODO: Implement using org.kohsuke.github.GitHub:
     * <pre>
     *   GitHub github = GitHub.connectUsingOAuth(accessToken);
     *   GHUser ghUser = github.getUser(githubUsername);
     *   Map<String, GHRepository> repos = ghUser.getRepositories();
     *   return repos.values().stream()
     *       .map(this::mapToDomain)
     *       .toList();
     * </pre>
     *
     * @param githubUsername the GitHub login of the target user
     * @return list of domain {@link Repository} objects
     */
    @Override
    public List<Repository> fetchRepositories(String githubUsername) {
        log.info("Fetching repositories from GitHub for username={}", githubUsername);

        // TODO: Replace stub with real GitHub API call using org.kohsuke.github.GitHub.
        //       Use GitHub.connectUsingOAuth(accessToken) — note that the access token
        //       should be passed in (not stored in a field) to support multi-user contexts.
        //       Consider caching results with a short TTL to respect GitHub rate limits.
        throw new UnsupportedOperationException(
                "TODO: implement fetchRepositories for githubUsername=" + githubUsername);
    }

    /**
     * Fetches the authenticated GitHub user's identity using the provided access token.
     *
     * TODO: Implement using org.kohsuke.github.GitHub:
     * <pre>
     *   GitHub github = GitHub.connectUsingOAuth(accessToken);
     *   GHMyself myself = github.getMyself();
     *   return new GithubUserInfo(
     *       String.valueOf(myself.getId()),
     *       myself.getLogin(),
     *       myself.getEmail()
     *   );
     * </pre>
     *
     * @param accessToken a valid GitHub OAuth access token
     * @return a {@link GithubUserInfo} record with the user's identity fields
     */
    @Override
    public GithubUserInfo fetchUserInfo(String accessToken) {
        log.info("Fetching GitHub user info using access token");

        // TODO: Replace stub with real GitHub API call.
        //       GitHub.connectUsingOAuth(accessToken) → github.getMyself()
        throw new UnsupportedOperationException(
                "TODO: implement fetchUserInfo using org.kohsuke.github.GitHub.connectUsingOAuth");
    }

    // -------------------------------------------------------------------------
    // Private mapping helpers
    // -------------------------------------------------------------------------

    /**
     * Maps a {@code org.kohsuke.github.GHRepository} to a domain {@link Repository}.
     *
     * TODO: Uncomment and adapt once the GitHub API integration is implemented.
     *
     * private Repository mapToDomain(org.kohsuke.github.GHRepository ghRepo) {
     *     return Repository.builder()
     *             .name(ghRepo.getName())
     *             .fullName(ghRepo.getFullName())
     *             .description(ghRepo.getDescription())
     *             .language(ghRepo.getLanguage())
     *             .stars(ghRepo.getStargazersCount())
     *             .forks(ghRepo.getForksCount())
     *             .lastActivity(ghRepo.getPushedAt().toInstant()
     *                     .atZone(java.time.ZoneId.systemDefault()).toLocalDateTime())
     *             .selected(false)
     *             .build();
     * }
     */
}
