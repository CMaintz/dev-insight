package com.devinsight.adapter.in.rest;

import com.devinsight.domain.port.in.GetDashboardUseCase;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import java.util.UUID;

/**
 * REST adapter for the authenticated developer dashboard endpoint.
 *
 * The dashboard aggregates the user's repositories and most recent analyses
 * into a single response optimised for the frontend dashboard page.
 *
 * This endpoint requires authentication — see
 * {@link com.devinsight.infrastructure.config.SecurityConfig}.
 */
@Slf4j
@RestController
@RequestMapping("/dashboard")
@RequiredArgsConstructor
public class DashboardController {

    private final GetDashboardUseCase getDashboardUseCase;

    /**
     * Returns the dashboard payload for the authenticated user.
     *
     * GET /dashboard
     *
     * TODO: Resolve the authenticated userId from the JWT SecurityContext
     *       (via a custom Principal or Authentication object) rather than
     *       accepting it as a query parameter.  This is a temporary stub.
     *
     * @param userId the internal UUID of the authenticated user (temporary — see TODO)
     * @return 200 OK with the {@link GetDashboardUseCase.DashboardData} payload
     */
    @GetMapping
    public ResponseEntity<GetDashboardUseCase.DashboardData> getDashboard(
            @RequestParam("userId") UUID userId) {

        log.info("Dashboard request for userId={}", userId);

        GetDashboardUseCase.DashboardData data = getDashboardUseCase.getDashboard(userId);
        return ResponseEntity.ok(data);
    }
}
