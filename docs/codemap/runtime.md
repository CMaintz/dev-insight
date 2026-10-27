---
sha: c3c2a9a5f9b0ae37b52d76325a7b6ed294280136
slice: runtime
kind: lens
---

# Runtime, config and deploy

## Processes and entrypoints

### API host
- One ASP.NET Core process, targeting `net10.0` (`backend/Directory.Build.props`), with its SDK pinned in `backend/global.json` and `backend/mise.toml`. `backend/src/DevInsight.Api/Program.cs#Program` registers services through `backend/src/DevInsight.Infrastructure/DependencyInjection.cs#DependencyInjection.AddInfrastructure` and `backend/src/DevInsight.Api/Infrastructure/ApiServices.cs#ApiServices.AddApiServices`. `Program` is declared `public partial` so the integration tests can host it (`backend/tests/DevInsight.Api.Tests/ApiFactory.cs#ApiFactory`).
- Pipeline order in `backend/src/DevInsight.Api/Program.cs#Program`: forwarded headers, exception handler, status-code pages, security headers (`backend/src/DevInsight.Api/Infrastructure/ApiServices.cs#SecurityHeaders.UseSecurityHeaders`), default and static files, CORS only when `Frontend:Url` yields an origin (`backend/src/DevInsight.Api/Auth/SignInCodes.cs#FrontendOptions.Origin`), authentication, authorization, then the rate limiter (`backend/src/DevInsight.Api/Infrastructure/ApiServices.cs#ApiServices.AddRateLimits`).
- Mapped surface: OpenAPI, the Scalar reference, `/health`, the auth and API endpoint groups (`backend/src/DevInsight.Api/Endpoints/AuthEndpoints.cs`, `backend/src/DevInsight.Api/Endpoints/ApiEndpoints.cs`). Unknown `/api/*` paths return 404 and any other path falls back to `index.html`, the SPA (`backend/src/DevInsight.Api/Program.cs#Program`).
- On startup the host applies EF Core migrations unless `Database:MigrateOnStartup` is false (default true) (`backend/src/DevInsight.Api/Program.cs#Program`, `backend/src/DevInsight.Api/appsettings.json`).
- Local `dotnet run` listens on `http://localhost:5080` in `Development` (`backend/src/DevInsight.Api/Properties/launchSettings.json`). The container listens on 8080 in `Production` (`Dockerfile`).

### Hosted services (in-process)
- `backend/src/DevInsight.Infrastructure/AnalysisRuns/AnalysisWorker.cs#AnalysisWorker` and `backend/src/DevInsight.Infrastructure/AnalysisRuns/AnalysisWorker.cs#ScheduledReanalysisService` are registered as hosted services by `backend/src/DevInsight.Infrastructure/DependencyInjection.cs#DependencyInjection.AddAnalysisWorker`.
- `AnalysisWorker` first re-queues unfinished runs (`backend/src/DevInsight.Infrastructure/AnalysisRuns/AnalysisWorker.cs#AnalysisWorker.RecoverUnfinishedRunsAsync`), then drains an in-memory unbounded channel (`backend/src/DevInsight.Infrastructure/AnalysisRuns/AnalysisWorker.cs#ChannelAnalysisQueue`) with parallelism `AnalysisWorker:MaxConcurrency`, running each run in its own DI scope.
- `ScheduledReanalysisService` ticks every `AnalysisWorker:ScheduledReanalysisHours` and queues analyses of every user's selected repositories (`backend/src/DevInsight.Infrastructure/AnalysisRuns/AnalysisWorker.cs#ScheduledReanalysisService.ReanalyseAllAsync`). Both services return immediately when `AnalysisWorker:Enabled` is false. The schedule is also disabled when the hours value is 0 or less (`backend/src/DevInsight.Infrastructure/AnalysisRuns/AnalysisWorker.cs#AnalysisWorkerOptions`).
- The runtime image installs the `git` CLI for repository cloning (`Dockerfile`, `backend/src/DevInsight.Infrastructure/Git/GitSnapshotSource.cs#GitAnalysisOptions.GitExecutable`).

### Angular app
- Bootstraps `App` with `frontend/src/app/app.config.ts#appConfig` (`frontend/src/main.ts`). An app initializer fetches `config.json` relative to `document.baseURI` before the first HTTP call (`frontend/src/app/core/config/app-config.ts#loadAppConfig`). Its `apiBaseUrl` is prefixed to API requests by `frontend/src/app/core/http/api.interceptors.ts#apiBaseUrlInterceptor`.
- The committed `frontend/public/config.json` has an empty `apiBaseUrl`, so requests go same-origin. `ng serve` proxies `/api` and `/health` to `http://localhost:5080` (`frontend/proxy.conf.json`, `frontend/angular.json`).
- There are two hosting modes. In the first, the API serves the built SPA from `wwwroot` (`Dockerfile`). In the second, GitHub Pages serves it under `/dev-insight/` with a rewritten `config.json` (`.github/workflows/deploy-pages.yml`, `frontend/angular.json`).

### Scripts
- `scripts/setup-azure.sh`: an interactive wizard that provisions Azure, OIDC deploy credentials, GitHub repo variables, the Bicep deployment and Pages (see Deploy).
- `foundry-verb-wrap` runs a gate verb and appends one JSON line of telemetry to `FOUNDRY_TELEMETRY`, passing the exit code through. It is not in the repo: the `setup:foundry-scripts` task in `backend/mise.toml` and `frontend/mise.toml` fetches it from CMaintz/foundry at `FOUNDRY_SCRIPTS_REF` into a cache dir on `mise install`. `scripts/foundry-loop-report` summarises that log.
- `scripts/ruleset_guard.py`: checks whether a ratchet-baseline change loosens it.

## Configuration

.NET binds configuration sections to options classes. Environment variables use `__` for `:` (`.env.example`).

| Section / key | Bound in | Defaults | Set by |
|---|---|---|---|
| `ConnectionStrings:DevInsight` | `backend/src/DevInsight.Infrastructure/DependencyInjection.cs#DependencyInjection.RequiredConnectionString` (throws if empty) | empty in `backend/src/DevInsight.Api/appsettings.json`; localhost in `backend/src/DevInsight.Api/appsettings.Development.json` | `docker-compose.yml` (`db` service); `infra/main.bicep` secret `db-connection` |
| `Jwt:*` (Issuer, Audience, SigningKey, LifetimeDays) | `backend/src/DevInsight.Api/Auth/SessionTokens.cs#JwtOptions` (key at least 32 bytes, `backend/src/DevInsight.Api/Auth/SessionTokens.cs#JwtOptions.GetSecurityKey`) | `backend/src/DevInsight.Api/appsettings.json`; dev key in `backend/src/DevInsight.Api/appsettings.Development.json` | `.env.example`; `infra/main.bicep` secret `jwt-signing-key` |
| `GitHub:*` (ClientId, ClientSecret, CallbackUrl, IncludePrivateRepositories, ProductName) | `backend/src/DevInsight.Infrastructure/GitHub/GitHubOptions.cs#GitHubOptions` | `backend/src/DevInsight.Api/appsettings.json`; CallbackUrl `:4200` in `backend/src/DevInsight.Api/appsettings.Development.json` | `.env.example`; CallbackUrl in `docker-compose.yml`; id, secret and CallbackUrl (the API's own FQDN) in `infra/main.bicep` |
| `Frontend:Url` | `backend/src/DevInsight.Api/Auth/SignInCodes.cs#FrontendOptions` (drives CORS in `backend/src/DevInsight.Api/Infrastructure/ApiServices.cs#ApiServices.AddCrossOriginSpa` and the post-login redirect) | empty (same-origin) | `infra/main.bicep` from param `frontendUrl` (`infra/main.bicepparam`) |
| `GitAnalysis:*` (GitExecutable, CloneBaseUrl, WorkRoot, MaxRepositorySizeMb, MaxCommits, CloneTimeoutSeconds) | `backend/src/DevInsight.Infrastructure/Git/GitSnapshotSource.cs#GitAnalysisOptions` | `backend/src/DevInsight.Api/appsettings.json` and class defaults | not set by compose or Bicep |
| `AnalysisWorker:*` (Enabled, MaxConcurrency, ScheduledReanalysisHours) | `backend/src/DevInsight.Infrastructure/AnalysisRuns/AnalysisWorker.cs#AnalysisWorkerOptions` | `backend/src/DevInsight.Api/appsettings.json` (true, 2, 24) | `MaxConcurrency=1` in `infra/main.bicep` |
| `AiFeedback:*` (ApiKey, Model, MaxFindings) | `backend/src/DevInsight.Infrastructure/Ai/ClaudeFeedbackGenerator.cs#AiFeedbackOptions` (empty key disables AI feedback) | `backend/src/DevInsight.Api/appsettings.json` | `.env.example`; `infra/main.bicep` secret `anthropic-api-key` only when a key is given |
| `Database:MigrateOnStartup` | `backend/src/DevInsight.Api/Program.cs#Program` | true | not overridden |
| `ASPNETCORE_URLS`, `ASPNETCORE_ENVIRONMENT` | ASP.NET host | `Dockerfile` (`http://+:8080`, `Production`); `backend/src/DevInsight.Api/Properties/launchSettings.json` (`Development`) | |

- Compose loads `.env` into the `app` service and overrides the connection string and the GitHub callback (`docker-compose.yml`). `.env` is excluded from the image (`.dockerignore`).
- Bicep receives secrets as `@secure()` params and stores them as Container Apps secrets that env vars reference by `secretRef` (`infra/main.bicep#api`). `infra/main.bicepparam` reads them from `DEVINSIGHT_IMAGE`, `DEVINSIGHT_DEPLOY_POSTGRES`, `DEVINSIGHT_DB_CONNECTION`, `DEVINSIGHT_DB_PASSWORD`, `DEVINSIGHT_GITHUB_CLIENT_ID`, `DEVINSIGHT_GITHUB_CLIENT_SECRET`, `DEVINSIGHT_JWT_SIGNING_KEY` and `DEVINSIGHT_ANTHROPIC_API_KEY`. `scripts/setup-azure.sh` exports those, saved in `.env.azure` (overridable with `DEVINSIGHT_ENV_FILE`).
- Data-protection keys persist to the database (`backend/src/DevInsight.Infrastructure/DependencyInjection.cs#DependencyInjection.AddPersistence`).
- The API project declares a `UserSecretsId` (`backend/src/DevInsight.Api/DevInsight.Api.csproj`).
- Test-only: `DEVINSIGHT_TEST_POSTGRES` points the API tests at an existing server instead of Testcontainers (`backend/tests/DevInsight.Api.Tests/ApiFactory.cs#ApiFactory.ExternalServerVariable`).
- Frontend runtime config is only `apiBaseUrl` (`frontend/src/app/core/config/app-config.ts#AppConfigData`).

## Build

- The root `mise.toml` defines `fix`, `lint`, `typecheck`, `test`, `audit` and `gate`, each running the same task in `backend/` and then `frontend/`.
- Backend (`backend/mise.toml`, solution `backend/DevInsight.slnx`):
  - `lint`: `dotnet format --verify-no-changes` plus the `smells` task (habit-hooks, configured in `backend/.habit-hooks/config.toml`).
  - `typecheck`: `dotnet build -warnaserror`.
  - `test`: `dotnet test` on Microsoft.Testing.Platform (`backend/global.json`) with an 85% merged line-coverage floor via ReportGenerator (`backend/dotnet-tools.json`).
  - `audit`: `dotnet list package --vulnerable`.
  - `gate` wraps each step in the cached `foundry-verb-wrap`.
  - Package versions are central (`backend/Directory.Packages.props`). SonarAnalyzer is referenced only when a `CodeAnalysisRuleSet` is passed (`backend/Directory.Build.props`).
- Frontend (`frontend/mise.toml` pins Node; npm scripts in `frontend/package.json`):
  - `lint`: `ng lint` with zero warnings, a Prettier check and `frontend/scripts/check-function-length.mjs`.
  - `typecheck`: `tsc` over the app and spec tsconfigs.
  - `test`: Vitest via `ng test`, with coverage thresholds in `frontend/angular.json`, plus `node --test` on the build scripts.
  - `audit`: `npm audit --audit-level=low`.
  - `build:pages` builds `production,pages` (base href `/dev-insight/`) and runs `frontend/scripts/spa-fallback.mjs#writeSpaFallback`, which copies `index.html` to `404.html` and writes `.nojekyll`.
- The container build is multi-stage (`Dockerfile`): a `node` stage runs `npm ci` and `npm run build`, an `sdk` stage runs `dotnet publish` on the API project, and an `aspnet` runtime stage combines the publish output with the SPA in `wwwroot`.

## CI

Workflows under `.github/workflows/`:

- `.github/workflows/gate.yml`: on PRs and pushes to `main`, calls the pinned reusable `CMaintz/foundry` gate workflow once for `backend` (stack `dotnet`) and once for `frontend` (stack `ts`).
- `.github/workflows/security.yml`: on PRs and pushes to `main`, calls the Foundry security workflow (gitleaks, ruleset guard, semgrep). It passes the gate-defining paths as `ruleset_paths`.
- `.github/workflows/ratchet.yml`: on PRs, posts a report on how `backend/.habit-hooks/snooze.json` and `frontend/.habit-hooks/snooze.json` moved.
- `.github/workflows/bootstrap.yml`: on a daily schedule or manual dispatch, regenerates each package's snooze baseline and opens a PR using a GitHub App key (`BOOTSTRAP_APP_PRIVATE_KEY`, `BOOTSTRAP_APP_CLIENT_ID`).
- `.github/workflows/infra.yml`: on changes under `infra/`, compiles `infra/main.bicep` with a pinned Bicep version.
- Dependency updates come from Renovate, extending the Foundry preset. A package rule disables updates for `Microsoft.CodeAnalysis.CSharp`, which is pinned to the version `Microsoft.EntityFrameworkCore.Design` depends on (`renovate.json`, `backend/Directory.Packages.props`).

## Deploy

- Local stack: `docker-compose.yml` runs `db` (`postgres:17-alpine` with a healthcheck and the `pgdata` volume) and `app` (built from `Dockerfile`, port 8080, waits for a healthy `db`).
- API to Azure: `.github/workflows/deploy-api.yml` runs on pushes to `main` that touch `backend/**`, `frontend/**` or `Dockerfile`, or on manual dispatch.
  1. Builds the image and pushes it to `ghcr.io/cmaintz/devinsight-api` tagged with the commit SHA and `latest`.
  2. Logs in to Azure over OIDC, but only when the `AZURE_CLIENT_ID` variable is set.
  3. Runs `az containerapp update --image` on `ca-devinsight-api`. A missing Container App is skipped with a notice.
- Infrastructure: `infra/main.bicep` defines a Log Analytics workspace, a Container Apps environment, an optional PostgreSQL 17 Flexible Server with database and an "allow Azure services" firewall rule (`infra/main.bicep#postgres`), and the Container App (`infra/main.bicep#api`):
  - external ingress on 8080
  - liveness probe on `/health`
  - 0 to 1 replicas
  - outputs `apiUrl` and `githubCallbackUrl`

  No workflow runs `az deployment`; `scripts/setup-azure.sh` deploys the template (`deploy_infra`) with `infra/main.bicepparam`.
- `scripts/setup-azure.sh` also:
  - creates the Entra app, the federated credential for the `production` environment and the Contributor role assignment
  - sets the repo variables `AZURE_*` and `DEVINSIGHT_API_URL`
  - dispatches `deploy-api.yml`
  - asks the user to make the GHCR package public
  - forces a new revision after the OAuth secrets change
  - switches Pages to workflow builds and dispatches `deploy-pages.yml`
- SPA to GitHub Pages: `.github/workflows/deploy-pages.yml` runs on pushes to `main` under `frontend/**`, and is skipped while `DEVINSIGHT_API_URL` is unset. It installs Node through `jdx/mise-action`, runs `npm run build:pages`, overwrites `dist/devinsight/browser/config.json` with `apiBaseUrl` set to `DEVINSIGHT_API_URL`, and deploys with `actions/deploy-pages`.

## Unknowns
- Where is the pre-push hook that runs `mise run gate` installed? `mise.toml` mentions it, but the tree has no hook file or hook manager config.
- What exact steps do the reusable `CMaintz/foundry` gate, security, ratchet and bootstrap workflows run? They live outside this repo, so `.github/workflows/gate.yml` only shows their inputs.
- With 0 replicas when idle and migrations applied on startup, does a cold start ever exceed the 10 s liveness `initialDelaySeconds` in `infra/main.bicep#api`? This cannot be determined from the code.
- Where do analysis clones go in Azure? `GitAnalysis:WorkRoot` is unset, so they use the system temp directory, and `infra/main.bicep` declares no volume. Does that ephemeral container storage fit `MaxRepositorySizeMb`?
- `GitHub:IncludePrivateRepositories` is set only in `.env.example`, not in `infra/main.bicep`. Is the deployed API meant to stay public-repos-only?
- Is `docs/DEPLOYMENT.md` in step with `scripts/setup-azure.sh`? `infra/main.bicepparam` points to the doc, but this map did not check it.
