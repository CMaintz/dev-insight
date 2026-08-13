package com.devinsight.adapter.out.persistence;

import com.devinsight.adapter.out.persistence.entity.RepositoryEntity;
import com.devinsight.adapter.out.persistence.repository.RepositoryJpaRepository;
import com.devinsight.domain.model.Repository;
import com.devinsight.domain.port.out.RepositoryPersistencePort;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Component;

import java.util.List;
import java.util.Optional;
import java.util.UUID;

/**
 * Secondary (driven) adapter implementing {@link RepositoryPersistencePort} using JPA.
 *
 * Handles mapping between the {@link Repository} domain model and the
 * {@link RepositoryEntity} JPA entity.  Inline mapping is used here for simplicity.
 *
 * TODO: use MapStruct here — generate a RepositoryMapper annotated with
 *       {@code @Mapper(componentModel = "spring")} and inject it via constructor.
 */
@Component
@RequiredArgsConstructor
public class RepositoryPersistenceAdapter implements RepositoryPersistencePort {

    private final RepositoryJpaRepository repositoryJpaRepository;

    @Override
    public List<Repository> findByUserId(UUID userId) {
        return repositoryJpaRepository.findByUserId(userId).stream()
                .map(this::toDomain)
                .toList();
    }

    @Override
    public Repository save(Repository repo) {
        RepositoryEntity entity = toEntity(repo);
        RepositoryEntity saved = repositoryJpaRepository.save(entity);
        return toDomain(saved);
    }

    @Override
    public Optional<Repository> findById(UUID id) {
        return repositoryJpaRepository.findById(id)
                .map(this::toDomain);
    }

    @Override
    public List<Repository> saveAll(List<Repository> repos) {
        List<RepositoryEntity> entities = repos.stream()
                .map(this::toEntity)
                .toList();
        return repositoryJpaRepository.saveAll(entities).stream()
                .map(this::toDomain)
                .toList();
    }

    // -------------------------------------------------------------------------
    // Mapping helpers — TODO: replace with MapStruct
    // -------------------------------------------------------------------------

    /**
     * Maps a {@link RepositoryEntity} to a {@link Repository} domain object.
     *
     * TODO: use MapStruct here
     */
    private Repository toDomain(RepositoryEntity entity) {
        return Repository.builder()
                .id(entity.getId())
                .userId(entity.getUserId())
                .name(entity.getName())
                .fullName(entity.getFullName())
                .description(entity.getDescription())
                .language(entity.getLanguage())
                .stars(entity.getStars())
                .forks(entity.getForks())
                .lastActivity(entity.getLastActivity())
                .selected(entity.isSelected())
                .build();
    }

    /**
     * Maps a {@link Repository} domain object to a {@link RepositoryEntity}.
     *
     * TODO: use MapStruct here
     */
    private RepositoryEntity toEntity(Repository repo) {
        return RepositoryEntity.builder()
                .id(repo.getId())
                .userId(repo.getUserId())
                .name(repo.getName())
                .fullName(repo.getFullName())
                .description(repo.getDescription())
                .language(repo.getLanguage())
                .stars(repo.getStars())
                .forks(repo.getForks())
                .lastActivity(repo.getLastActivity())
                .selected(repo.isSelected())
                .build();
    }
}
