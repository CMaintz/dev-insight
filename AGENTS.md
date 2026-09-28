# AGENTS.md

DevInsight: .NET 10 API (`backend/`, hexagonal) + Angular 22 SPA (`frontend/`). Deployed as GitHub Pages (SPA)
+ Azure Container Apps (API) — see `docs/DEPLOYMENT.md`.

## Done means a green gate

`mise run gate` at the repository root is the oracle (lint → typecheck → test → audit, backend then frontend;
each package also has its own `mise run gate`). Work is done when it is green, not when it looks right.

- The backend API tests need PostgreSQL: Docker (Testcontainers) or `DEVINSIGHT_TEST_POSTGRES` holding a
  server connection string. Without either they skip, and the 75 % coverage floor then fails — a red `test`
  for that reason means "provide a database", not "lower the floor".
- `mise run fix` applies the mechanical fixes (formatting). Rule sets, thresholds and baselines change only in
  their own PR (CI's ruleset-guard enforces this).

## Invariants the code relies on

- **Dependencies point inward.** `Domain` references nothing; `Application` knows no adapter technology.
  `DevInsight.ArchitectureTests` enforces it.
- **Scores stay explainable.** Every scored metric carries `Points` and `Weight`, and a dimension's weights sum
  to 1 (tested). New metric keys go in `MetricKeys.cs` and get a label in `frontend/src/app/shared/format/metrics.ts`.
- **The HTTP contract lives in `docs/API.md`**; `frontend/src/app/core/models` mirrors it. Change all three together.
- **Auth is bearer-only.** The SPA and API are on different sites, so cookies are unusable; sign-in hands the SPA
  a one-time code that it exchanges for a JWT.
- **No inline scripts in `index.html`** — the CSP (`script-src 'self'`) blocks them in production.
- **EF migrations** are generated, never hand-written:
  `ConnectionStrings__DevInsight="Host=x" dotnet ef migrations add <Name> -p src/DevInsight.Infrastructure -s src/DevInsight.Api -o Persistence/Migrations` (from `backend/`).

## Decisions

The owner reviews every autonomous choice. Record each one — feature, technology, threshold, UX — in
`docs/DECISIONS.md` (frontend: `docs/DECISIONS-frontend.md`) with the alternative you rejected and why.
