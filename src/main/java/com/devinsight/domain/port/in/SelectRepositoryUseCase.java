package com.devinsight.domain.port.in;

import com.devinsight.domain.model.Repository;

import java.util.UUID;

/**
 * Input port for toggling whether a repository is featured in the user's portfolio.
 *
 * Only repositories with {@code selected = true} are rendered on the public
 * portfolio page.
 */
public interface SelectRepositoryUseCase {

    /**
     * Updates the {@code selected} flag on the given repository.
     *
     * @param repositoryId the internal {@link java.util.UUID} of the repository
     * @param selected     {@code true} to feature the repo in the portfolio,
     *                     {@code false} to hide it
     * @return the updated {@link Repository} domain object reflecting the change
     */
    Repository setSelected(UUID repositoryId, boolean selected);
}
