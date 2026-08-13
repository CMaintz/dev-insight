package com.devinsight.adapter.in.rest;

import com.devinsight.domain.port.in.LoginUseCase;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.util.Map;

/**
 * REST adapter (primary/driving adapter) for authentication endpoints.
 *
 * Handles the GitHub OAuth callback by receiving the short-lived authorisation
 * code, delegating the full OAuth flow to the {@link LoginUseCase}, and
 * returning the signed JWT to the client.
 */
@Slf4j
@RestController
@RequestMapping("/auth")
@RequiredArgsConstructor
public class AuthController {

    private final LoginUseCase loginUseCase;

    /**
     * GitHub OAuth callback endpoint.
     *
     * After the user authorises DevInsight on GitHub, GitHub redirects here
     * with a one-time {@code code} query parameter.  This endpoint exchanges
     * that code for a JWT and returns it to the caller.
     *
     * GET /auth/callback?code={code}
     *
     * @param code the one-time GitHub OAuth authorisation code
     * @return 200 OK with a JSON body {@code {"token": "<jwt>", "username": "<github_username>"}}
     */
    @GetMapping("/callback")
    public ResponseEntity<Map<String, String>> callback(@RequestParam("code") String code) {
        log.info("Received GitHub OAuth callback");

        LoginUseCase.LoginResult result = loginUseCase.login(code);

        // TODO: Consider redirecting to the frontend SPA with the JWT as a
        //       URL fragment or secure HttpOnly cookie rather than returning
        //       the token in the response body.
        return ResponseEntity.ok(Map.of(
                "token", result.jwt(),
                "username", result.user().getGithubUsername()
        ));
    }
}
