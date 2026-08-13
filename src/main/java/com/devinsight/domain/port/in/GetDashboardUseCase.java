package com.devinsight.domain.port.in;

import com.devinsight.domain.model.Analysis;
import com.devinsight.domain.model.Repository;

import java.util.List;
import java.util.UUID;

/**
 * Input port for retrieving the authenticated user's personalised dashboard data.
 *
 * The dashboard aggregates the user's imported repositories and their most
 * recent analysis results in a single query to minimise round trips from
 * the frontend.
 */
public interface GetDashboardUseCase {

    /**
     * Collects and returns the dashboard payload for the given user.
     *
     * @param userId the internal {@link java.util.UUID} of the authenticated user
     * @return a {@link DashboardData} record containing all repositories and
     *         the most recent analyses
     */
    DashboardData getDashboard(UUID userId);

    /**
     * Aggregate view of data required to render the developer dashboard.
     *
     * @param repositories    all repositories imported by the user
     * @param recentAnalyses  the most recent analysis per repository (or globally)
     */
    record DashboardData(List<Repository> repositories, List<Analysis> recentAnalyses) {}
}
