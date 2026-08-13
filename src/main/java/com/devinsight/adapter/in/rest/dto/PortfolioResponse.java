package com.devinsight.adapter.in.rest.dto;

import com.devinsight.domain.model.Analysis;
import com.devinsight.domain.model.Repository;
import com.devinsight.domain.model.User;
import com.devinsight.domain.port.in.GetPortfolioUseCase;

import java.util.List;
import java.util.UUID;

/**
 * REST response record for the public developer portfolio endpoint.
 *
 * Aggregates user profile data with a summary of each featured repository
 * and its latest overall analysis score.
 *
 * @param userId          internal UUID of the portfolio owner
 * @param githubUsername  GitHub login of the portfolio owner
 * @param bio             optional biography
 * @param linkedinUrl     optional LinkedIn profile URL
 * @param repositories    summarised view of each selected repository
 */
public record PortfolioResponse(
        UUID userId,
        String githubUsername,
        String bio,
        String linkedinUrl,
        List<RepositorySummary> repositories
) {

    /**
     * Compact summary of a repository for use within a portfolio response.
     *
     * @param id           internal UUID of the repository
     * @param name         short repository name (without owner prefix)
     * @param language     primary programming language
     * @param stars        number of GitHub stars at the time of last import
     * @param overallScore overall analysis score (0–100); -1 if never analysed
     */
    public record RepositorySummary(
            UUID id,
            String name,
            String language,
            int stars,
            int overallScore
    ) {}

    /**
     * Convenience factory method that maps a {@link GetPortfolioUseCase.PortfolioData}
     * domain aggregate into a {@link PortfolioResponse}.
     *
     * The overall score for each repository is taken from the first matching
     * {@link Analysis} by repository ID; defaults to -1 if not yet analysed.
     *
     * @param data the domain portfolio data
     * @return a new {@link PortfolioResponse}
     */
    public static PortfolioResponse from(GetPortfolioUseCase.PortfolioData data) {
        User user = data.user();

        List<RepositorySummary> summaries = data.selectedRepositories().stream()
                .map(repo -> {
                    int score = data.analyses().stream()
                            .filter(a -> a.getRepositoryId().equals(repo.getId()))
                            .mapToInt(Analysis::getOverallScore)
                            .findFirst()
                            .orElse(-1);
                    return new RepositorySummary(
                            repo.getId(),
                            repo.getName(),
                            repo.getLanguage(),
                            repo.getStars(),
                            score
                    );
                })
                .toList();

        return new PortfolioResponse(
                user.getId(),
                user.getGithubUsername(),
                user.getBio(),
                user.getLinkedinUrl(),
                summaries
        );
    }
}
