package com.devinsight.domain.port.in;

import com.devinsight.domain.model.Analysis;
import com.devinsight.domain.model.Repository;
import com.devinsight.domain.model.User;

import java.util.List;
import java.util.UUID;

/**
 * Input port for retrieving a developer's public portfolio.
 *
 * Unlike the dashboard (which is private and authenticated), the portfolio
 * endpoint is publicly accessible and shows only the repositories the user
 * has explicitly selected to feature, together with their latest analyses.
 */
public interface GetPortfolioUseCase {

    /**
     * Assembles and returns the public portfolio for the given user.
     *
     * @param userId the internal {@link java.util.UUID} of the target user
     * @return a {@link PortfolioData} record ready to be serialised and returned
     *         to unauthenticated visitors
     */
    PortfolioData getPortfolio(UUID userId);

    /**
     * Aggregate view of data required to render the public developer portfolio.
     *
     * @param user                  the profile data for the developer
     * @param selectedRepositories  repositories the developer has chosen to feature
     * @param analyses              the most recent analysis for each selected repository
     */
    record PortfolioData(User user, List<Repository> selectedRepositories, List<Analysis> analyses) {}
}
