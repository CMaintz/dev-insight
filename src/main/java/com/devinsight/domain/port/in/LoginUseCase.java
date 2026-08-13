package com.devinsight.domain.port.in;

import com.devinsight.domain.model.User;

/**
 * Input port (primary/driving port) for user authentication via GitHub OAuth.
 *
 * The caller provides the one-time OAuth authorisation code received from
 * GitHub's callback redirect; the use case returns a JWT and the resolved
 * user record.
 */
public interface LoginUseCase {

    /**
     * Exchanges the GitHub OAuth authorisation {@code code} for an access token,
     * resolves or creates the local {@link User} record, and generates a signed JWT.
     *
     * @param code the one-time OAuth code from GitHub's callback query parameter
     * @return a {@link LoginResult} containing the signed JWT and the resolved user
     */
    LoginResult login(String code);

    /**
     * Value object returned after a successful login.
     *
     * @param jwt  signed JWT that the client should store and send as a Bearer token
     * @param user the resolved (or newly created) {@link User} domain object
     */
    record LoginResult(String jwt, User user) {}
}
