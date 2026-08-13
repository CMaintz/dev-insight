package com.devinsight.adapter.in.rest;

import com.devinsight.adapter.in.rest.dto.PortfolioResponse;
import com.devinsight.domain.port.in.GetPortfolioUseCase;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import java.util.UUID;

/**
 * REST adapter for the public developer portfolio endpoint.
 *
 * This endpoint is intentionally unauthenticated — anyone with the URL
 * can view a developer's public portfolio.  Access control is configured
 * in {@link com.devinsight.infrastructure.config.SecurityConfig}.
 */
@Slf4j
@RestController
@RequestMapping("/portfolio")
@RequiredArgsConstructor
public class PortfolioController {

    private final GetPortfolioUseCase getPortfolioUseCase;

    /**
     * Returns the public portfolio for the specified developer.
     *
     * GET /portfolio/{userId}
     *
     * This endpoint is publicly accessible (no authentication required).
     * It returns only repositories the user has explicitly selected to feature,
     * along with their analysis scores.
     *
     * @param userId the internal UUID of the portfolio owner
     * @return 200 OK with the {@link PortfolioResponse} payload
     */
    @GetMapping("/{userId}")
    public ResponseEntity<PortfolioResponse> getPortfolio(
            @PathVariable("userId") UUID userId) {

        log.info("Public portfolio request for userId={}", userId);

        GetPortfolioUseCase.PortfolioData data = getPortfolioUseCase.getPortfolio(userId);
        return ResponseEntity.ok(PortfolioResponse.from(data));
    }
}
