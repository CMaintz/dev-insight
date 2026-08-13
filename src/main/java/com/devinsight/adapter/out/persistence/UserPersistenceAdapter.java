package com.devinsight.adapter.out.persistence;

import com.devinsight.adapter.out.persistence.entity.UserEntity;
import com.devinsight.adapter.out.persistence.repository.UserJpaRepository;
import com.devinsight.domain.model.User;
import com.devinsight.domain.port.out.UserPersistencePort;
import lombok.RequiredArgsConstructor;
import org.springframework.stereotype.Component;

import java.util.Optional;
import java.util.UUID;

/**
 * Secondary (driven) adapter implementing {@link UserPersistencePort} using JPA.
 *
 * Handles mapping between the {@link User} domain model and the {@link UserEntity}
 * JPA entity.  Inline mapping is used here for simplicity.
 *
 * TODO: use MapStruct here — generate a UserMapper interface annotated with
 *       {@code @Mapper(componentModel = "spring")} and inject it via constructor.
 */
@Component
@RequiredArgsConstructor
public class UserPersistenceAdapter implements UserPersistencePort {

    private final UserJpaRepository userJpaRepository;

    @Override
    public Optional<User> findByGithubId(String githubId) {
        return userJpaRepository.findByGithubId(githubId)
                .map(this::toDomain);
    }

    @Override
    public User save(User user) {
        UserEntity entity = toEntity(user);
        UserEntity saved = userJpaRepository.save(entity);
        return toDomain(saved);
    }

    @Override
    public Optional<User> findById(UUID id) {
        return userJpaRepository.findById(id)
                .map(this::toDomain);
    }

    // -------------------------------------------------------------------------
    // Mapping helpers — TODO: replace with MapStruct
    // -------------------------------------------------------------------------

    /**
     * Maps a {@link UserEntity} to a {@link User} domain object.
     *
     * TODO: use MapStruct here
     */
    private User toDomain(UserEntity entity) {
        return User.builder()
                .id(entity.getId())
                .email(entity.getEmail())
                .githubId(entity.getGithubId())
                .githubUsername(entity.getGithubUsername())
                .linkedinUrl(entity.getLinkedinUrl())
                .bio(entity.getBio())
                .createdAt(entity.getCreatedAt())
                .build();
    }

    /**
     * Maps a {@link User} domain object to a {@link UserEntity}.
     *
     * TODO: use MapStruct here
     */
    private UserEntity toEntity(User user) {
        return UserEntity.builder()
                .id(user.getId())
                .email(user.getEmail())
                .githubId(user.getGithubId())
                .githubUsername(user.getGithubUsername())
                .linkedinUrl(user.getLinkedinUrl())
                .bio(user.getBio())
                .createdAt(user.getCreatedAt())
                .build();
    }
}
