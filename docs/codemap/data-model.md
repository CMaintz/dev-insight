---
sha: d4240bcfccce19669016317887401a47a79c4403
slice: data-model
kind: lens
---

## Store and context

- One PostgreSQL database through EF Core: `DevInsightDbContext` is registered with Npgsql and a snake_case naming convention, reading the `DevInsight` connection string (`backend/src/DevInsight.Infrastructure/DependencyInjection.cs#AddPersistence`, `backend/src/DevInsight.Infrastructure/DependencyInjection.cs#RequiredConnectionString`).
- The context exposes nine sets, applies every `IEntityTypeConfiguration` in its assembly, and stores the five domain enums as strings (max 16 or 32 chars) (`backend/src/DevInsight.Infrastructure/Persistence/DevInsightDbContext.cs#DevInsightDbContext.ConfigureConventions`).
- Entity mapping lives in one file, one configuration class per entity (`backend/src/DevInsight.Infrastructure/Persistence/Configurations/EntityConfigurations.cs`).
- The context is also the unit of work: one `SaveChangesAsync` commits everything the stores tracked (`backend/src/DevInsight.Application/Abstractions/Stores.cs#IUnitOfWork`, `backend/src/DevInsight.Infrastructure/Persistence/DevInsightDbContext.cs#DevInsightDbContext`). Application code reaches tables only through the store interfaces (`backend/src/DevInsight.Application/Abstractions/Stores.cs`), implemented in `backend/src/DevInsight.Infrastructure/Persistence/Stores/EfStores.cs` and `backend/src/DevInsight.Infrastructure/Persistence/Stores/EfAnalysisStore.cs#EfAnalysisStore`.
- Primary keys are UUIDv7 from `backend/src/DevInsight.Domain/Common/Ids.cs#Ids.New`. Entities with a `UserId` implement `backend/src/DevInsight.Domain/Common/IUserOwned.cs#IUserOwned`.
- Value-object collections map to `jsonb` through `backend/src/DevInsight.Infrastructure/Persistence/JsonColumn.cs#JsonColumn.HasJsonConversion`; `List<string>`/`List<Guid>` properties map to Postgres `text[]`/`uuid[]` columns (`backend/src/DevInsight.Infrastructure/Persistence/Migrations/20260927213310_InitialCreate.cs#InitialCreate.Up`).

## Entities

| Entity (table) | Fields of note | Owner (writer) |
|---|---|---|
| `User` (`users`) `backend/src/DevInsight.Domain/Users/User.cs#User` | `GitHubId` (unique index, column `github_id`); `Login` (non-unique index); `CommitEmails` (`text[]`, built from GitHub noreply plus verified emails in `User.BuildCommitEmails`); profile `Bio`, `LinkedInUrl`, `IsPortfolioPublic` | api-auth |
| `GitHubCredential` (`github_credentials`) `backend/src/DevInsight.Infrastructure/Persistence/DevInsightDbContext.cs#GitHubCredential` | keyed by `UserId`; `ProtectedToken` is the OAuth token encrypted with Data Protection (`backend/src/DevInsight.Infrastructure/Security/ProtectedGitHubCredentialStore.cs#ProtectedGitHubCredentialStore.SaveAsync`) | api-auth (sign-in is the only writer, through the backend-core store) |
| `Repository` (`repositories`) `backend/src/DevInsight.Domain/Repositories/Repository.cs#Repository` | unique (`UserId`, `GitHubRepoId`); `Languages` as `jsonb`; `IsSelected` defaults true only for non-fork repos the user owns (`Repository.Import`); `FullName` is computed and not mapped | analysis |
| `Analysis` (`analyses`) `backend/src/DevInsight.Domain/Analyses/Analysis.cs#Analysis` | append-only snapshot per repository and `Scope`; four scores; `HeadCommitSha`; `Timeline` (`ActivityWeek` list) and `LargestFiles` (`FileSize` list) as `jsonb` (`backend/src/DevInsight.Domain/Analyses/ActivityWeek.cs`); index (`RepositoryId`, `Scope`, `CreatedAt`) | analysis |
| `AnalysisMetric` (`analysis_metrics`) `backend/src/DevInsight.Domain/Analyses/AnalysisMetric.cs#AnalysisMetric` | unique (`AnalysisId`, `Name`); `Category`; `Value`; `IncludedInScore`, `Points`, `Weight` set only by `AnalysisMetric.Scored` | analysis |
| `Feedback` (`feedback`) `backend/src/DevInsight.Domain/Analyses/Feedback.cs#Feedback` | `Type` (`RuleBased`/`Ai`), `Severity`, `Category`, `Source` (rule id or `ai`), `IsStrength` | analysis |
| `AnalysisRun` (`analysis_runs`) `backend/src/DevInsight.Domain/Analyses/AnalysisRun.cs#AnalysisRun` | `Status` state machine Queued -> Running -> Succeeded/Failed, with `Requeue` back to Queued; `AnalysisId` set on success; `Error` truncated to 1000 chars; indexes (`RepositoryId`, `Scope`, `Status`) and (`UserId`, `RequestedAt`) | analysis (plus requeue in backend-core) |
| `Project` (`projects`) `backend/src/DevInsight.Domain/Projects/Project.cs#Project` | `ImageUrls` (`text[]`, at most 10, https only); `LinkedRepositoryIds` (`uuid[]`, de-duplicated); `SortOrder` with index (`UserId`, `SortOrder`) | analysis |
| `DataProtectionKey` (`data_protection_keys`) | ASP.NET Core Data Protection key ring, persisted via `PersistKeysToDbContext` (`backend/src/DevInsight.Infrastructure/DependencyInjection.cs#AddPersistence`) | framework, configured in backend-core |

Enums: `AnalysisScope` (`Repo`, `UserContribution`) and `MetricCategory` in `backend/src/DevInsight.Domain/Analyses/Enums.cs`; `AnalysisRunStatus` in `backend/src/DevInsight.Domain/Analyses/AnalysisRun.cs#AnalysisRunStatus`; `FeedbackType` and `Severity` in `backend/src/DevInsight.Domain/Analyses/Feedback.cs`.

## Relationships

All foreign keys cascade on delete (`backend/src/DevInsight.Infrastructure/Persistence/Configurations/EntityConfigurations.cs`):

- `users` 1-n `repositories` (`RepositoryConfiguration.Configure`), 1-n `projects` (`ProjectConfiguration.Configure`), 1-1 `github_credentials` (`GitHubCredentialConfiguration.Configure`).
- `repositories` 1-n `analyses` (`AnalysisConfiguration.Configure`) and 1-n `analysis_runs` (`AnalysisRunConfiguration.Configure`).
- `analyses` 1-n `analysis_metrics` and 1-n `feedback`, both loaded through backing fields (`AnalysisConfiguration.Configure`) and included together by `backend/src/DevInsight.Infrastructure/Persistence/Stores/EfAnalysisStore.cs#EfAnalysisStore.WithDetails`.
- Not mapped as foreign keys: `AnalysisRun.UserId`, `AnalysisRun.AnalysisId` (`backend/src/DevInsight.Infrastructure/Persistence/Configurations/EntityConfigurations.cs#AnalysisRunConfiguration.Configure`) and `Project.LinkedRepositoryIds`, which is a `uuid[]` column checked against the user's repositories in application code (`backend/src/DevInsight.Application/Projects/ProjectUseCases.cs#ProjectUseCases.EnsureOwnRepositoriesAsync`).

## Who writes what

- `User`: created or re-synced on GitHub login (`backend/src/DevInsight.Application/Auth/LoginWithGitHub.cs#LoginWithGitHub.ExecuteAsync`); profile fields updated by `backend/src/DevInsight.Application/Profile/ProfileUseCases.cs#ProfileUseCases.UpdateAsync`.
- `GitHubCredential`: upserted by `backend/src/DevInsight.Infrastructure/Security/ProtectedGitHubCredentialStore.cs#ProtectedGitHubCredentialStore.SaveAsync`, invoked from `backend/src/DevInsight.Application/Auth/LoginWithGitHub.cs#LoginWithGitHub.ExecuteAsync`.
- `Repository`: imported or refreshed, with languages, by `backend/src/DevInsight.Application/Repositories/RepositoryUseCases.cs#ImportRepositories.Upsert` and `ImportRepositories.FetchLanguagesAsync`; `IsSelected` toggled by `backend/src/DevInsight.Application/Repositories/RepositoryUseCases.cs#RepositoryQueries.SetSelectedAsync`.
- `AnalysisRun`: queued by `backend/src/DevInsight.Application/Analyses/RequestAnalysis.cs#RequestAnalysis.ExecuteAsync` (returns the unfinished run instead when one exists for the repository and scope); started, succeeded or failed by `backend/src/DevInsight.Application/Analyses/RunAnalysis.cs#RunAnalysis.ExecuteAsync`; requeued at startup by `backend/src/DevInsight.Infrastructure/AnalysisRuns/AnalysisWorker.cs#AnalysisWorker.RecoverUnfinishedRunsAsync`. `ScheduledReanalysisService.ReanalyseAllAsync` in the same file queues runs through `RequestAnalysis`.
- `Analysis`, `AnalysisMetric`, `Feedback`: created together in `backend/src/DevInsight.Application/Analyses/RunAnalysis.cs#RunAnalysis.AnalyzeAsync` (metrics from the engine result, rule feedback from `FeedbackRuleSet`, AI feedback from `RunAnalysis.GenerateAiFeedbackAsync`). No code path updates or deletes an analysis (`backend/src/DevInsight.Application/Abstractions/Stores.cs#IAnalysisStore`).
- `Project`: created, updated and deleted by `backend/src/DevInsight.Application/Projects/ProjectUseCases.cs#ProjectUseCases`.
- insights only reads: `backend/src/DevInsight.Application/Dashboard/GetDashboard.cs#GetDashboard` and `backend/src/DevInsight.Application/Portfolio/GetPortfolio.cs#GetPortfolio` take stores but no `IUnitOfWork`.
- Reads and updates of user-owned rows go through the ownership check `backend/src/DevInsight.Application/Common/Errors.cs#Ownership.OwnedBy`.

## Schema and migrations

- One migration, `backend/src/DevInsight.Infrastructure/Persistence/Migrations/20260927213310_InitialCreate.cs#InitialCreate`, creates all nine tables and indexes; the model snapshot is `backend/src/DevInsight.Infrastructure/Persistence/Migrations/DevInsightDbContextModelSnapshot.cs#DevInsightDbContextModelSnapshot`.
- Migrations are applied on API startup when `Database:MigrateOnStartup` is true, its default and its value in `backend/src/DevInsight.Api/appsettings.json` (`backend/src/DevInsight.Api/Program.cs`).

## Frontend shapes

- The Angular app mirrors the API DTOs, not the tables, in `frontend/src/app/core/models/api.models.ts` (for example `Repository`, `Analysis`, `AnalysisRun`, `Project`, `Portfolio`), with enums as lower-case string unions such as `RunStatus`.

## Unknowns

- Is there any path that deletes a `User` or a `Repository`? Neither `IUserStore` nor `IRepositoryStore` exposes a remove (`backend/src/DevInsight.Application/Abstractions/Stores.cs`), so the cascades are only reachable from outside the application layer.
- What keeps `Project.LinkedRepositoryIds` consistent if a linked repository row goes away, given it is not a foreign key?
- Does the snapshot match the current model exactly (no pending model changes)? That needs `dotnet ef migrations has-pending-model-changes`, which was not run.
- Is `Database:MigrateOnStartup` overridden in any deployed environment (env var `Database__MigrateOnStartup`)? No such override is in the repo.
