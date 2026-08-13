package com.devinsight.infrastructure.config;

import com.devinsight.domain.model.User;
import io.jsonwebtoken.Claims;
import io.jsonwebtoken.JwtException;
import io.jsonwebtoken.Jwts;
import io.jsonwebtoken.security.Keys;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Component;

import javax.crypto.SecretKey;
import java.nio.charset.StandardCharsets;
import java.util.Date;

/**
 * Service responsible for generating and validating signed JWTs.
 *
 * Uses the JJWT library (io.jsonwebtoken) with an HMAC-SHA-256 signature.
 * The secret and expiration time are injected from application configuration
 * ({@code jwt.secret} and {@code jwt.expiration-ms}).
 *
 * Token claims:
 *   sub    — the user's GitHub ID (stable identifier)
 *   userId — the internal DevInsight UUID
 *   iss    — "devinsight"
 *   iat    — issued-at timestamp
 *   exp    — expiration timestamp
 *
 * Security notes:
 *   - The {@code jwt.secret} value must be at least 256 bits (32 characters) long.
 *   - In production, supply the secret via environment variable (JWT_SECRET).
 *   - Never log the secret or the full token value.
 */
@Slf4j
@Component
public class JwtService {

    private static final String ISSUER = "devinsight";
    private static final String CLAIM_USER_ID = "userId";

    @Value("${jwt.secret}")
    private String secret;

    @Value("${jwt.expiration-ms:86400000}")
    private long expirationMs;

    /**
     * Generates a signed JWT for the given authenticated user.
     *
     * The token encodes the user's {@code githubId} as the subject and
     * includes the internal {@code userId} as a custom claim.
     *
     * @param user the authenticated {@link User}
     * @return a signed, compact JWT string
     */
    public String generateToken(User user) {
        Date now = new Date();
        Date expiry = new Date(now.getTime() + expirationMs);

        return Jwts.builder()
                .issuer(ISSUER)
                .subject(user.getGithubId())
                .claim(CLAIM_USER_ID, user.getId().toString())
                .issuedAt(now)
                .expiration(expiry)
                .signWith(buildSigningKey())
                .compact();
    }

    /**
     * Extracts the GitHub ID ({@code sub} claim) from a JWT.
     *
     * @param token a signed JWT string
     * @return the GitHub ID encoded in the token's subject claim
     * @throws JwtException if the token is invalid or expired
     */
    public String extractGithubId(String token) {
        return parseClaims(token).getSubject();
    }

    /**
     * Validates that the given JWT is well-formed, signed with the correct key,
     * and has not expired.
     *
     * @param token the JWT string to validate
     * @return {@code true} if the token is valid; {@code false} otherwise
     */
    public boolean isValid(String token) {
        try {
            parseClaims(token);
            return true;
        } catch (JwtException | IllegalArgumentException e) {
            log.debug("JWT validation failed: {}", e.getMessage());
            return false;
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /**
     * Parses and verifies the JWT, returning the extracted {@link Claims}.
     *
     * @param token the JWT string
     * @return the verified {@link Claims}
     * @throws JwtException if the token is invalid, expired, or tampered with
     */
    private Claims parseClaims(String token) {
        return Jwts.parser()
                .verifyWith(buildSigningKey())
                .build()
                .parseSignedClaims(token)
                .getPayload();
    }

    /**
     * Builds the HMAC-SHA signing key from the configured secret string.
     *
     * TODO: In production, consider loading the key from a key store or
     *       secret manager rather than a plain string property.
     *
     * @return a {@link SecretKey} suitable for HS256/HS512 signing
     */
    private SecretKey buildSigningKey() {
        return Keys.hmacShaKeyFor(secret.getBytes(StandardCharsets.UTF_8));
    }
}
