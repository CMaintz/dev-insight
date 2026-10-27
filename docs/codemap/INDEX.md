---
sha: c3c2a9a5f9b0ae37b52d76325a7b6ed294280136
slice: index
kind: index
---

# DevInsight codemap

DevInsight imports a developer's GitHub repositories, runs scored analyses of them with
rule-based and optional AI feedback, and shows the results on a private dashboard and a
public portfolio. A .NET 10 API (`backend/src/DevInsight.Api/Program.cs`) in clean-architecture
layers serves an Angular SPA (`frontend/src/main.ts`).

## Domains

| Slice | File | Purpose |
| --- | --- | --- |
| analysis | [analysis.md](analysis.md) | Repository import, selection, queued analysis runs and their scoring, portfolio projects |
| api-auth | [api-auth.md](api-auth.md) | API host and pipeline, GitHub OAuth sign-in, JWT sessions, the user's profile |
| backend-core | [backend-core.md](backend-core.md) | Adapters behind the Application ports: EF Core, Octokit, git CLI, Anthropic, the run queue and workers |
| insights | [insights.md](insights.md) | Read-side dashboard and public portfolio, backend use cases and their pages |
| frontend-core | [frontend-core.md](frontend-core.md) | SPA shell: bootstrap, runtime config, routes, API clients, interceptors, toasts, theme, scope |
| frontend-pages | [frontend-pages.md](frontend-pages.md) | Landing, sign-in callback, repository detail, settings and not-found pages, shared UI and styles |

Lenses: [data-model.md](data-model.md) (entities, schema, migrations, ownership),
[integrations.md](integrations.md) (GitHub, git, Anthropic and other external systems),
[runtime.md](runtime.md) (processes, configuration, build, CI, deploy).

## Key cross-domain flows

### Sign in with GitHub
frontend-pages -> api-auth -> frontend-core. Detail in [api-auth.md](api-auth.md).
1. `frontend/src/app/features/landing/landing.page.ts#LandingPage` starts sign-in through `frontend/src/app/core/auth/session.store.ts#SessionStore.signIn`.
2. The API's GitHub callback runs `backend/src/DevInsight.Application/Auth/LoginWithGitHub.cs#LoginWithGitHub.ExecuteAsync`, which registers or syncs the `backend/src/DevInsight.Domain/Users/User.cs#User` and stores the GitHub token.
3. A one-time code (`backend/src/DevInsight.Api/Auth/SignInCodes.cs#SignInCodes`) is exchanged by the SPA's callback page for a JWT signed by `backend/src/DevInsight.Api/Auth/SessionTokens.cs#SessionTokenIssuer.Issue`.

### Analyse a repository
frontend-core -> analysis -> backend-core -> integrations. Detail in [analysis.md](analysis.md).
1. `frontend/src/app/core/analysis/run-tracker.ts#RunTracker.analyse` posts one run per scope.
2. `backend/src/DevInsight.Application/Analyses/RequestAnalysis.cs#RequestAnalysis.ExecuteAsync` saves a queued `backend/src/DevInsight.Domain/Analyses/AnalysisRun.cs#AnalysisRun` and puts it on `backend/src/DevInsight.Infrastructure/AnalysisRuns/AnalysisWorker.cs#ChannelAnalysisQueue`.
3. `backend/src/DevInsight.Infrastructure/AnalysisRuns/AnalysisWorker.cs#AnalysisWorker` hands it to `backend/src/DevInsight.Application/Analyses/RunAnalysis.cs#RunAnalysis.ExecuteAsync`, which clones with `backend/src/DevInsight.Infrastructure/Git/GitSnapshotSource.cs#GitSnapshotSource.CaptureAsync`, scores with `backend/src/DevInsight.Domain/Analyses/Engine/AnalysisEngine.cs#AnalysisEngine.Analyze` and optionally adds feedback from `backend/src/DevInsight.Infrastructure/Ai/ClaudeFeedbackGenerator.cs#ClaudeFeedbackGenerator.GenerateAsync`.
4. The SPA polls the run through `frontend/src/app/core/analysis/run-poller.ts#RunPoller.poll`.

### Scheduled re-analysis
backend-core -> analysis. Detail in [backend-core.md](backend-core.md).
1. `backend/src/DevInsight.Infrastructure/AnalysisRuns/AnalysisWorker.cs#ScheduledReanalysisService` calls `backend/src/DevInsight.Application/Analyses/RequestAnalysis.cs#RequestAnalysis.ExecuteForSelectedAsync` for every user on a timer, and the runs follow the flow above.

### Dashboard and portfolio
insights reads what analysis writes. Detail in [insights.md](insights.md).
1. `backend/src/DevInsight.Application/Dashboard/GetDashboard.cs#GetDashboard.ExecuteAsync` and `backend/src/DevInsight.Application/Portfolio/GetPortfolio.cs#GetPortfolio` aggregate the user's selected repositories and their latest analyses.

## Where things live

- Entrypoints: API host `backend/src/DevInsight.Api/Program.cs`, HTTP routes `backend/src/DevInsight.Api/Endpoints/ApiEndpoints.cs#ApiEndpoints` and `backend/src/DevInsight.Api/Endpoints/AuthEndpoints.cs#AuthEndpoints`, background workers `backend/src/DevInsight.Infrastructure/AnalysisRuns/AnalysisWorker.cs`, SPA routes `frontend/src/app/app.routes.ts#routes`.
- Data model: `backend/src/DevInsight.Infrastructure/Persistence/DevInsightDbContext.cs#DevInsightDbContext`, `backend/src/DevInsight.Infrastructure/Persistence/Configurations/EntityConfigurations.cs`, migrations in `backend/src/DevInsight.Infrastructure/Persistence/Migrations/`. See [data-model.md](data-model.md).
- Dependency wiring: `backend/src/DevInsight.Infrastructure/DependencyInjection.cs#DependencyInjection.AddInfrastructure`.
- Config: `backend/src/DevInsight.Api/appsettings.json`, SPA runtime config `frontend/public/config.json`, deployed settings `infra/main.bicep`. See [runtime.md](runtime.md).
- Tests: `backend/tests/` (Api, Application, Architecture, Domain, Infrastructure), frontend helpers in `frontend/src/testing/`.
- CI and deploy: `.github/workflows/gate.yml`, `.github/workflows/deploy-api.yml`, `.github/workflows/deploy-pages.yml`, `infra/main.bicep`, `docker-compose.yml`.

## Known unknowns

- Is the API ever run with more than one replica? The run queue and sign-in codes live in process memory ([analysis.md](analysis.md), [api-auth.md](api-auth.md), [backend-core.md](backend-core.md)).
- Can two concurrent requests for the same repository and scope both queue a run? No unique constraint on unfinished runs was found ([analysis.md](analysis.md)).
- Is anything ever deleted? No store exposes a remove for users or repositories, and `Project.LinkedRepositoryIds` is not a foreign key ([data-model.md](data-model.md)).
- How does the SPA handle the 7-day JWT expiring, beyond the generic 401 sign-out? ([api-auth.md](api-auth.md))
- Are the AI feedback model and `GitHub:IncludePrivateRepositories` defaults what production runs with? Neither is set in `infra/main.bicep` ([integrations.md](integrations.md), [runtime.md](runtime.md)).
