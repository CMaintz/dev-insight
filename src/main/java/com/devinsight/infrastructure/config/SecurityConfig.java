package com.devinsight.infrastructure.config;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.config.annotation.web.configurers.AbstractHttpConfigurer;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.web.SecurityFilterChain;

/**
 * Spring Security configuration for DevInsight.
 *
 * Configures a stateless JWT-based security model:
 *   - CSRF protection is disabled (safe for stateless REST APIs with JWTs).
 *   - Sessions are set to STATELESS — no server-side session state is maintained.
 *   - Public paths (/auth/**, /portfolio/**) are accessible without authentication.
 *   - All other endpoints require a valid JWT Bearer token.
 *
 * TODO: Add a {@code JwtAuthenticationFilter} that:
 *         1. Extracts the Bearer token from the Authorization header.
 *         2. Validates the token using {@link JwtService}.
 *         3. Loads the user and sets the {@link org.springframework.security.core.Authentication}
 *            in the {@link org.springframework.security.core.context.SecurityContextHolder}.
 *       Register it with:
 *         http.addFilterBefore(jwtAuthFilter, UsernamePasswordAuthenticationFilter.class);
 *
 * TODO: Configure CORS to allow requests from the frontend origin (e.g. localhost:3000
 *       in development, the production domain in production).
 */
@Configuration
@EnableWebSecurity
public class SecurityConfig {

    /**
     * Configures the main {@link SecurityFilterChain} for the application.
     *
     * @param http the {@link HttpSecurity} builder provided by Spring Security
     * @return the configured {@link SecurityFilterChain}
     * @throws Exception if any configuration step fails
     */
    @Bean
    public SecurityFilterChain securityFilterChain(HttpSecurity http) throws Exception {
        http
            // Disable CSRF — not needed for stateless REST APIs backed by JWTs
            .csrf(AbstractHttpConfigurer::disable)

            // Stateless session management — Spring Security will not create HttpSessions
            .sessionManagement(session ->
                    session.sessionCreationPolicy(SessionCreationPolicy.STATELESS))

            // Authorization rules
            .authorizeHttpRequests(auth -> auth
                    // Public endpoints: OAuth callback and public portfolio pages
                    .requestMatchers("/auth/**").permitAll()
                    .requestMatchers("/portfolio/**").permitAll()
                    // Everything else requires an authenticated user
                    .anyRequest().authenticated()
            );

        // TODO: Uncomment once JwtAuthenticationFilter is implemented:
        // http.addFilterBefore(jwtAuthenticationFilter, UsernamePasswordAuthenticationFilter.class);

        return http.build();
    }
}
