package com.devinsight.domain.service;

import com.devinsight.domain.model.User;
import com.devinsight.domain.port.in.LoginUseCase;
import com.devinsight.domain.port.out.GitHubPort;
import com.devinsight.domain.port.out.UserPersistencePort;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.stereotype.Service;

/**
 * Domain service implementing the {@link LoginUseCase}.
 *
 * Orchestrates the GitHub OAuth flow: exchanges the authorisation code for an
 * access token, fetches the authenticated user's GitHub profile, resolves or
 * creates the local user record, and issues a signed JWT.
 */
@Slf4j
@Service
@RequiredArgsConstructor
public class LoginService implements LoginUseCase {

    private final GitHubPort gitHubPort;
    private final UserPersistencePort userPersistencePort;

    /**
     * Full login flow:
     * 1. Exchange the OAuth code for a GitHub access token.
     * 2. Fetch the GitHub user's identity using the access token.
     * 3. Resolve an existing DevInsight user or create a new one.
     * 4. Generate and return a signed JWT.
     *
     * @param code the one-time GitHub OAuth authorisation code
     * @return {@link LoginResult} containing the JWT and the resolved user
     */
    @Override
    public LoginResult login(String code) {
        log.info("Processing GitHub OAuth login with authorisation code");

        // TODO: Exchange 'code' for a GitHub access token via an HTTP POST to
        //       https://github.com/login/oauth/access_token using clientId + clientSecret.
        //       Consider injecting an OAuth2-aware RestClient / WebClient here.
        String accessToken = exchangeCodeForToken(code);

        // Fetch GitHub identity using the access token
        GitHubPort.GithubUserInfo userInfo = gitHubPort.fetchUserInfo(accessToken);
        log.debug("Fetched GitHub user info for username={}", userInfo.username());

        // Resolve existing user or create a new one
        User user = userPersistencePort.findByGithubId(userInfo.githubId())
                .orElseGet(() -> {
                    log.info("Creating new user for githubId={}", userInfo.githubId());
                    User newUser = User.builder()
                            .githubId(userInfo.githubId())
                            .githubUsername(userInfo.username())
                            .email(userInfo.email())
                            .build();
                    return userPersistencePort.save(newUser);
                });

        // TODO: Generate a signed JWT using JwtService (inject via constructor).
        //       Pass the resolved user so the token can encode githubId as the subject.
        String jwt = generateJwt(user);

        return new LoginResult(jwt, user);
    }

    /**
     * Exchanges the GitHub OAuth authorisation code for an access token.
     *
     * TODO: Implement actual HTTP call to https://github.com/login/oauth/access_token.
     *       Parse the 'access_token' field from the response (form-encoded or JSON).
     *       Throw an appropriate exception (e.g. AuthenticationException) on failure.
     *
     * @param code the one-time authorisation code
     * @return the GitHub access token string
     */
    private String exchangeCodeForToken(String code) {
        // TODO: Replace this stub with a real RestClient/WebClient call.
        throw new UnsupportedOperationException(
                "TODO: implement OAuth code→token exchange for code=" + code);
    }

    /**
     * Generates a signed JWT for the given user.
     *
     * TODO: Inject JwtService and call jwtService.generateToken(user).
     *       Remove this helper method once JwtService is wired in.
     *
     * @param user the authenticated user
     * @return signed JWT string
     */
    private String generateJwt(User user) {
        // TODO: delegate to JwtService.generateToken(user) once it is injected.
        throw new UnsupportedOperationException(
                "TODO: implement JWT generation for userId=" + user.getId());
    }
}
