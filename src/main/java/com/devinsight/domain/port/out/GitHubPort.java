package com.devinsight.domain.port.out;

import com.devinsight.domain.model.Repository;

import java.util.List;

/**
 * Output port (secondary/driven port) for interacting with the GitHub API.
 *
 * The domain uses this interface to fetch repository data and authenticated
 * user information without being coupled to any specific GitHub client library.
 * The concrete adapter (e.g. using org.kohsuke.github) lives in the adapter layer.
 */
public interface GitHubPort {

    /**
     * Fetches all public repositories (and authorised private ones) for the
     * given GitHub username.
     *
     * @param githubUsername the GitHub login of the target user
     * @return list of {@link Repository} domain objects mapped from the GitHub API response
     */
    List<Repository> fetchRepositories(String githubUsername);

    /**
     * Exchanges a GitHub OAuth access token for the authenticated user's
     * basic profile information.
     *
     * @param accessToken a valid GitHub OAuth access token
     * @return a {@link GithubUserInfo} record with the user's identity fields
     */
    GithubUserInfo fetchUserInfo(String accessToken);

    /**
     * Minimal user identity data returned by the GitHub user info endpoint.
     *
     * @param githubId the stable GitHub node/account ID (not the login)
     * @param username the GitHub login (may change over time, unlike the ID)
     * @param email    the primary email; may be {@code null} if set to private on GitHub
     */
    record GithubUserInfo(String githubId, String username, String email) {}
}
