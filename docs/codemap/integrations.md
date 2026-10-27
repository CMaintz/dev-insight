---
sha: d4240bcfccce19669016317887401a47a79c4403
slice: integrations
kind: lens
---

Options classes bind in `backend/src/DevInsight.Infrastructure/DependencyInjection.cs#DependencyInjection.AddExternalServices` and `backend/src/DevInsight.Api/Infrastructure/ApiServices.cs#ApiServices.AddSignIn`; env vars use the `Section__Key` form documented in `.env.example`. Defaults live in `backend/src/DevInsight.Api/appsettings.json`; deployed values are set in `infra/main.bicep` and `docker-compose.yml`.

## GitHub OAuth (inbound callback + outbound token exchange)

- Direction: browser is redirected out to GitHub, GitHub redirects back in to `GET /api/auth/github/callback`, the API calls GitHub to exchange the code.
- Owner: api-auth (endpoints, use case); adapter in backend-core.
- Flow: the SPA sends the browser to `/api/auth/github/login` (`frontend/src/app/core/auth/session.store.ts#gitHubLoginUrl`); `backend/src/DevInsight.Api/Endpoints/AuthEndpoints.cs#AuthEndpoints.StartGitHubSignIn` redirects to the URL built by `backend/src/DevInsight.Infrastructure/GitHub/OctokitGitHubGateway.cs#OctokitGitHubGateway.GetAuthorizationUrl`, with state from `backend/src/DevInsight.Api/Auth/OAuthState.cs#OAuthState.Begin`. Scopes are `read:user` and `user:email`, plus `repo` when private repositories are enabled.
- Callback: `backend/src/DevInsight.Api/Endpoints/AuthEndpoints.cs#AuthEndpoints.CompleteGitHubSignInAsync` checks state (`backend/src/DevInsight.Api/Auth/OAuthState.cs#OAuthState.Complete`), runs `backend/src/DevInsight.Application/Auth/LoginWithGitHub.cs#LoginWithGitHub.ExecuteAsync` (code exchange via `backend/src/DevInsight.Infrastructure/GitHub/OctokitGitHubGateway.cs#OctokitGitHubGateway.ExchangeCodeForTokenAsync`, identity via `backend/src/DevInsight.Infrastructure/GitHub/OctokitGitHubGateway.cs#OctokitGitHubGateway.GetIdentityAsync`), stores the token, then redirects to the SPA with a one-time code (`backend/src/DevInsight.Api/Auth/SignInCodes.cs#FrontendOptions.CallbackUrl`).
- Config: `GitHub:ClientId`, `GitHub:ClientSecret`, `GitHub:CallbackUrl`, `GitHub:IncludePrivateRepositories`, `GitHub:ProductName` (`backend/src/DevInsight.Infrastructure/GitHub/GitHubOptions.cs#GitHubOptions`); `Frontend:Url` for the redirect back (`backend/src/DevInsight.Api/Auth/SignInCodes.cs#FrontendOptions`).

## GitHub REST API (outbound, via Octokit)

- Owner: backend-core adapter `backend/src/DevInsight.Infrastructure/GitHub/OctokitGitHubGateway.cs#OctokitGitHubGateway` behind the port `backend/src/DevInsight.Application/Abstractions/Ports.cs#IGitHubGateway`; callers in api-auth and analysis.
- Calls: current user and verified emails at sign-in (`backend/src/DevInsight.Infrastructure/GitHub/OctokitGitHubGateway.cs#OctokitGitHubGateway.GetIdentityAsync`); repositories the user owns, collaborates on or reaches through an org, with private ones filtered out unless enabled (`backend/src/DevInsight.Infrastructure/GitHub/OctokitGitHubGateway.cs#OctokitGitHubGateway.ListRepositoriesAsync`); per-repo language bytes (`backend/src/DevInsight.Infrastructure/GitHub/OctokitGitHubGateway.cs#OctokitGitHubGateway.GetLanguagesAsync`). The last two are driven by `backend/src/DevInsight.Application/Repositories/RepositoryUseCases.cs#ImportRepositories.ExecuteAsync` (`POST /api/repos/import`).
- Auth: a client per call with the user's stored token (`backend/src/DevInsight.Infrastructure/GitHub/OctokitGitHubGateway.cs#OctokitGitHubGateway.Authenticated`), read from `backend/src/DevInsight.Infrastructure/Security/ProtectedGitHubCredentialStore.cs#ProtectedGitHubCredentialStore.GetAsync`.
- Config: `GitHub:IncludePrivateRepositories`, `GitHub:ProductName` (user-agent product header) (`backend/src/DevInsight.Infrastructure/GitHub/GitHubOptions.cs#GitHubOptions`).
- Browser side: the SPA renders the GitHub `avatarUrl` as an image (`frontend/src/app/core/layout/app-header.html`, `frontend/src/app/features/portfolio/portfolio-hero.html`); the CSP allows `img-src https:` (`backend/src/DevInsight.Api/Infrastructure/ApiServices.cs#SecurityHeaders`).

## Git clone over HTTPS (outbound, git CLI)

- Owner: backend-core `backend/src/DevInsight.Infrastructure/Git/GitSnapshotSource.cs#GitSnapshotSource` behind `backend/src/DevInsight.Application/Abstractions/Ports.cs#IRepositorySnapshotSource`; called by analysis in `backend/src/DevInsight.Application/Analyses/RunAnalysis.cs#RunAnalysis.AnalyzeAsync`, which only runs on the background worker (`backend/src/DevInsight.Infrastructure/AnalysisRuns/AnalysisWorker.cs#AnalysisWorker`); the scheduled re-analysis only queues runs for it (`backend/src/DevInsight.Infrastructure/AnalysisRuns/AnalysisWorker.cs#ScheduledReanalysisService`).
- Behaviour: rejects repos over the size limit, single-branch clone of the default branch into a temp dir, then `git log --numstat` and `git ls-files`, then deletes the dir (`backend/src/DevInsight.Infrastructure/Git/GitSnapshotSource.cs#GitSnapshotSource.CaptureAsync`). The runtime image installs `git` for this (`Dockerfile`). The user's token goes in as an `http.https://github.com/.extraheader` via `GIT_CONFIG_*` env vars (`backend/src/DevInsight.Infrastructure/Git/GitSnapshotSource.cs#GitSnapshotSource.CloneAsync`); prompts are disabled with `GIT_TERMINAL_PROMPT=0` and `GCM_INTERACTIVE=never` (`backend/src/DevInsight.Infrastructure/Git/GitProcess.cs#GitProcess.StartInfo`).
- Config: `GitAnalysis:GitExecutable`, `CloneBaseUrl` (default `https://github.com`), `WorkRoot`, `MaxRepositorySizeMb`, `MaxCommits`, `CloneTimeoutSeconds` (`backend/src/DevInsight.Infrastructure/Git/GitSnapshotSource.cs#GitAnalysisOptions`); worker pacing from `AnalysisWorker:Enabled`, `MaxConcurrency`, `ScheduledReanalysisHours` (`backend/src/DevInsight.Infrastructure/AnalysisRuns/AnalysisWorker.cs#AnalysisWorkerOptions`).

## Anthropic Messages API (outbound, optional)

- Owner: backend-core `backend/src/DevInsight.Infrastructure/Ai/ClaudeFeedbackGenerator.cs#ClaudeFeedbackGenerator` behind `backend/src/DevInsight.Application/Abstractions/Ports.cs#IAiFeedbackGenerator`; called by analysis in `backend/src/DevInsight.Application/Analyses/RunAnalysis.cs#RunAnalysis.GenerateAiFeedbackAsync`, which skips it when disabled.
- Behaviour: no client is built when the key is empty, so `IsEnabled` is false (`backend/src/DevInsight.Infrastructure/Ai/ClaudeFeedbackGenerator.cs#ClaudeFeedbackGenerator.IsEnabled`). The request sends only metrics, scores, largest files and rule findings, with a JSON output schema and the `server-side-fallback-2026-07-01` beta (`backend/src/DevInsight.Infrastructure/Ai/ClaudeFeedbackGenerator.cs#ClaudeFeedbackGenerator.BuildRequest`). Refusals, rate limits and API errors return no findings (`backend/src/DevInsight.Infrastructure/Ai/ClaudeFeedbackGenerator.cs#ClaudeFeedbackGenerator.GenerateAsync`); the text is parsed by `backend/src/DevInsight.Infrastructure/Ai/AiFindingsParser.cs#AiFindingsParser.Parse`.
- Config: `AiFeedback:ApiKey`, `AiFeedback:Model` (default `claude-opus-5`), `AiFeedback:MaxFindings` (`backend/src/DevInsight.Infrastructure/Ai/ClaudeFeedbackGenerator.cs#AiFeedbackOptions`). In Azure the key is injected only when supplied (`infra/main.bicep`).

## PostgreSQL (outbound)

- Owner: backend-core. EF Core with Npgsql and snake_case naming, connection string `ConnectionStrings:DevInsight`, failing at resolve time when empty (`backend/src/DevInsight.Infrastructure/DependencyInjection.cs#DependencyInjection.AddPersistence`, `backend/src/DevInsight.Infrastructure/DependencyInjection.cs#DependencyInjection.RequiredConnectionString`).
- Migrations run at startup unless `Database:MigrateOnStartup` is false (`backend/src/DevInsight.Api/Program.cs`).
- Also stores the Data Protection key ring (`backend/src/DevInsight.Infrastructure/Persistence/DevInsightDbContext.cs#DevInsightDbContext.DataProtectionKeys`) used to encrypt GitHub tokens at rest (`backend/src/DevInsight.Infrastructure/Security/ProtectedGitHubCredentialStore.cs#ProtectedGitHubCredentialStore.SaveAsync`) and OAuth state (`backend/src/DevInsight.Api/Auth/OAuthState.cs#OAuthState`).
- Provisioned as `postgres:17-alpine` locally (`docker-compose.yml`) and an Azure Postgres flexible server when deployed (`infra/main.bicep`).

## SPA to API (inbound HTTP, possibly cross-origin)

- Owner: frontend-core (client), api-auth (server).
- The SPA fetches `config.json` at startup (`frontend/src/app/core/config/app-config.ts#loadAppConfig`, wired in `frontend/src/app/app.config.ts`); `apiBaseUrl` there (`frontend/public/config.json`) is prefixed onto `/api` calls (`frontend/src/app/core/http/api.interceptors.ts#apiBaseUrlInterceptor`). In dev, `/api` and `/health` proxy to `http://localhost:5080` (`frontend/proxy.conf.json`).
- Sign-in completes by posting the one-time code to `/api/auth/exchange` (`frontend/src/app/core/api/auth.api.ts#AuthApi.exchange`, from `frontend/src/app/features/auth-callback/auth-callback.page.ts#AuthCallbackPage`), answered by `backend/src/DevInsight.Api/Endpoints/AuthEndpoints.cs#AuthEndpoints.ExchangeSignInCodeAsync` with a JWT signed per `Jwt:Issuer`, `Jwt:Audience`, `Jwt:SigningKey`, `Jwt:LifetimeDays` (`backend/src/DevInsight.Api/Auth/SessionTokens.cs#JwtOptions`).
- CORS is enabled only when `Frontend:Url` is an absolute URL, allowing that origin (`backend/src/DevInsight.Api/Infrastructure/ApiServices.cs#ApiServices.AddCrossOriginSpa`, `backend/src/DevInsight.Api/Auth/SignInCodes.cs#FrontendOptions.Origin`).

## Scalar API reference UI (outbound from the browser)

- Owner: api-auth. Mapped in `backend/src/DevInsight.Api/Program.cs`; the CSP is skipped under `/scalar` because its UI loads from a CDN (`backend/src/DevInsight.Api/Infrastructure/ApiServices.cs#SecurityHeaders.UseSecurityHeaders`). Reads no config.

## Unknowns

- `CloneBaseUrl` can point at another host, but the auth header is scoped to `https://github.com/`: is the token sent at all when cloning from a GitHub Enterprise base URL?
- Which CDN host does the Scalar UI load from, and is it reachable where the API is deployed?
- Neither `infra/main.bicep` nor `docker-compose.yml` sets `GitHub__IncludePrivateRepositories` or any `GitAnalysis__*` key: are the defaults what production runs with?
- Is the default model id `claude-opus-5` and the `server-side-fallback-2026-07-01` beta accepted by the Anthropic API version the pinned SDK targets?
