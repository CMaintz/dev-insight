# DevInsight

> **A system that analyses developers' codebases and visualises their development over time.**

Most developer portfolios are static: a list of projects and a few adjectives. DevInsight is built on
the opposite idea — show *how* someone develops, backed by data they can't fake. It imports a user's
GitHub repositories, measures activity, commit habits, structure and quality, turns those measurements
into explainable scores and concrete feedback, and publishes the result as a living portfolio.

- **Deployment:** not live yet. The Pages + Azure Container Apps pipeline is in place (`deploy-pages`,
  `deploy-api`, `infra`) but skips until the Azure resources and `DEVINSIGHT_API_URL` are configured —
  run it locally for now (see below).
- **Private dashboard** — scores, activity over time, score evolution, language mix, commit-size
  distribution, and the most important feedback across your repositories.
- **Explainable analysis** — every score is the weighted sum of stored metrics; the UI shows each
  metric's value, points and weight. Two scopes: the *whole repository*, or *only your contributions*.
- **Feedback** — rule-based findings (vague commits, monoliths, missing tests, …) including what you do
  well, plus optional AI feedback grounded strictly in the measured data.
- **Public portfolio** at `/u/<github-login>` — your selected repositories scored on *your own* commits,
  projects with images, languages, activity and strengths. Private until you publish it.

## Architecture

```
frontend/   Angular 22 SPA (standalone components, signals, zoneless, Vitest)
backend/
  src/
    DevInsight.Domain          entities, analysis engine, scoring, feedback rules — no dependencies
    DevInsight.Application     use cases (UC1–UC6) and the ports they need
    DevInsight.Infrastructure  adapters: PostgreSQL (EF Core), GitHub (Octokit), git CLI, Claude, workers
    DevInsight.Api             ASP.NET Core minimal APIs, auth, OpenAPI, hosts the SPA
  tests/                       domain · application · infrastructure · architecture · API integration
docs/       API.md · DEPLOYMENT.md · DECISIONS.md (every design decision and why)
infra/      Bicep template for Azure Container Apps (+ PostgreSQL)
```

Hexagonal (ports & adapters): dependencies point inward, and an architecture test fails the build if the
domain or application layer ever references EF Core, ASP.NET Core, Octokit or the AI SDK.

**How an analysis runs.** `POST /api/analysis/run/{repo}` stores a queued run and returns `202`. A
background worker clones the repository with `git` (one clone replaces thousands of rate-limited API
calls), feeds the commit history and files at HEAD to the pure analysis engine, applies the feedback
rules (and optionally Claude), and stores an append-only analysis. The sequence of analyses *is* the
score history; a scheduled job re-analyses selected repositories daily so the history grows by itself.

### Scoring

| Dimension (weight) | Made of |
|---|---|
| **Activity** (30 %) | recency of the last commit · commits/week over 12 weeks · share of active weeks in the last 26 |
| **Structure** (30 %) | share of files > 500 LOC · monolith check · median file size · folder depth |
| **Quality** (40 %) | tests · commit-message quality · README · lint/format config · CI |

Exact formulas and thresholds: [`docs/DECISIONS.md`](docs/DECISIONS.md) §26 and `backend/src/DevInsight.Domain/Analyses/Engine`.

## Tech stack

.NET 10 (C# 14) · ASP.NET Core minimal APIs · EF Core 10 + PostgreSQL 17 · Octokit · Anthropic C# SDK ·
Azure Container Apps · Bicep · GitHub Actions (OIDC) · GitHub Pages ·
Angular 22 · ngx-charts · xUnit v3 · Shouldly · NetArchTest · Testcontainers · Vitest · Docker · mise.

## Running locally

Prerequisites: [mise](https://mise.jdx.dev) (installs the pinned .NET SDK and Node), Docker, and a
[GitHub OAuth App](https://github.com/settings/developers) with callback URL
`http://localhost:4200/api/auth/github/callback`.

```bash
mise install                                   # .NET 10.0.401 + Node 24.21.0
docker compose up -d db                        # PostgreSQL on :5432

cd backend
dotnet user-secrets --project src/DevInsight.Api set GitHub:ClientId     <client id>
dotnet user-secrets --project src/DevInsight.Api set GitHub:ClientSecret <client secret>
dotnet run --project src/DevInsight.Api        # API on :5080, migrates the database on startup

cd ../frontend
npm ci && npm start                            # SPA on :4200, proxies /api to :5080
```

Open http://localhost:4200, sign in with GitHub, import, analyse, publish.

**Production (planned):** the SPA targets GitHub Pages and the API Azure Container Apps — see
[`docs/DEPLOYMENT.md`](docs/DEPLOYMENT.md) and run `bash scripts/setup-azure.sh` once.

**Everything in one container:** `cp .env.example .env`, fill it in (callback URL
`http://localhost:8080/api/auth/github/callback`), then `docker compose up --build` → http://localhost:8080.

**AI feedback** is optional: set `AiFeedback:ApiKey` (Anthropic API key) and every analysis gets up to
five additional, data-grounded findings. Without it, feedback is rule-based only.

API reference: http://localhost:5080/scalar (OpenAPI at `/openapi/v1.json`); overview in [`docs/API.md`](docs/API.md).

## Testing

```bash
cd backend && dotnet test          # all backend suites (API tests need Docker, or DEVINSIGHT_TEST_POSTGRES)
cd frontend && npm test            # Vitest with coverage
```

The API integration tests boot the real application — auth, database, background worker — against a
disposable PostgreSQL and walk the whole journey: GitHub sign-in → import → analyse → dashboard →
publish → public portfolio, plus the security cases (forged OAuth state, open redirects, cross-user access).

## Status and roadmap

Implemented: UC1–UC6 from the specification, projects, scheduled snapshots, optional AI feedback.
Next candidates: GitLab provider, historical score back-fill from commit history, recruiter view,
CI coverage import. The original Spring Boot skeleton is in the history at commit `cb57e91`.
