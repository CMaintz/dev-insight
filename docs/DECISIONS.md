# Decision log — for review

Every choice below was made autonomously during the .NET rewrite (September 2026), without an
explicit instruction from you. Each entry says **what** was decided, **the alternative** that was
rejected, and **why**. Anything you disagree with is cheap to change now — tell me the number.

Legend: 🧭 product/feature · 🏗 architecture · 🧰 technology · 🔒 security · ⚙️ tooling/process · ⚠️ needs your action

---

## Process

1. ⚙️ **Rewrite on a branch, Java preserved by tag.** Work is on `feat/dotnet-rewrite`; the Spring Boot
   skeleton is tagged `java-skeleton` (local tag, not pushed). *Alt:* new repository. *Why:* history shows the pivot.
2. ⚙️ **Java sources removed** (`src/`, `pom.xml`) on your go-ahead; recoverable from the `java-skeleton` tag.
3. ⚙️ **Local commits only — nothing pushed, no PR opened.** Pushing/PR creation is outward-facing, so it waits for you.
4. ⚙️ **Brainstorming folder left untracked.** `additional-brainstorming-and-context/` holds 8 .docx files
   (5 near-duplicates). I read them all: the *Master Specification* already consolidates the other 7, and
   nothing in the copies is missing from it. I did not commit or delete them. *Suggestion:* keep only the
   Master Specification (maybe as Markdown under `docs/`) and delete the copies.
5. ⚙️ **Foundry wired after its .NET stack merged** (see §Foundry).
6. ⚙️ **Parallel build.** The Angular frontend was built by a sub-agent against `docs/API.md` while the
   backend was written; its own decisions are merged in §Frontend below.

## Stack

7. 🧰 **.NET 10 (LTS, SDK 10.0.401) / C# 14**, installed per-project via mise (`mise.toml`, `global.json`).
   *Alt:* .NET 9 (installed on your machine, but STS and out of support in 2026). *Why:* LTS, current.
8. 🧰 **Angular 22 (standalone, signals, zoneless, Vitest)** kept as the frontend. *Alt:* Blazor.
   *Why:* your TypeScript skills, the job market pairs ASP.NET Core + Angular often, and Blazor WASM
   is heavy for a public, SEO-relevant portfolio page. Node pinned to 24.21.0 (Angular 22 needs ≥ 22.22).
9. 🧰 **Minimal APIs** instead of MVC controllers. *Why:* less ceremony, idiomatic for new .NET services;
   endpoints are grouped per feature so it still reads like controllers.
10. 🧰 **EF Core 10 + Npgsql + snake_case naming**, **EF migrations** instead of Flyway-style SQL files.
    *Why:* idiomatic .NET; the schema is generated from the mapping, so they can't drift.
    Collections of value objects (timeline, largest files, languages) are `jsonb`; e-mail/URL/id lists are native Postgres arrays.
11. 🧰 **Octokit** for the GitHub API; **git CLI clone** for analysis (see #25).
12. 🧰 **xUnit v3 on Microsoft.Testing.Platform** (required by `dotnet test` on .NET 10), **Shouldly** for
    assertions (*Alt:* FluentAssertions — commercial licence since v8), **NetArchTest** for architecture rules,
    **Testcontainers** for PostgreSQL. Coverage via **coverlet.MTP**.
13. 🧰 **Central package management** (`Directory.Packages.props`) with transitive pinning, exact versions.
    Pinning surfaced a real conflict (Npgsql pulled EF Relational 10.0.4 vs 10.0.12) that is now fixed.
14. 🧰 **Scalar** for the API reference UI (`/scalar`), fed by the built-in OpenAPI document (`/openapi/v1.json`),
    exposed in all environments. *Alt:* Swashbuckle (no longer in the templates). *Why:* "documented API" is a
    stated job-readiness goal.

## Architecture

15. 🏗 **Four projects mirroring the hexagon:** `Domain` (entities, analysis engine, rules — zero dependencies),
    `Application` (use cases + ports), `Infrastructure` (adapters), `Api` (HTTP). Enforced by architecture tests.
16. 🏗 **Use-case classes instead of MediatR/CQRS.** *Why:* one class per use case is explicit and needs no
    library (MediatR also went commercial).
17. 🏗 **Unit of work = the DbContext**, exposed through `IUnitOfWork`; stores only add/query.
18. 🏗 **Application read models are returned to the API and mapped to DTOs there** (`Contracts/Mapping.cs`);
    the wire contract is documented in `docs/API.md`.
19. 🏗 **All API routes under `/api`** (the spec listed them without a prefix) so the SPA and API can share one origin.
20. 🏗 **The API can also serve the SPA** (`wwwroot`, SPA fallback) — used by the single-container Docker setup.
    *Superseded for production* by GitHub Pages + Azure (see #46, #55): the SPA is cross-origin there.
21. 🏗 **Analyses run in the background.** `POST /analysis/run` returns **202 + a run id** to poll; an in-process
    `Channel` queue feeds a `BackgroundService` (2 concurrent runs). Runs are persisted first and re-queued on
    restart. Requests are idempotent per repo+scope while one is pending. *Alt:* synchronous (cloning can take
    minutes), or Hangfire/queue broker (overkill for one instance).
22. 🏗 **Analyses are append-only snapshots** — score history is the list of past analyses (spec: "snapshot-based").
23. 🧭 **Scheduled re-analysis every 24 h** of every user's selected repositories, so score history grows on its
    own (configurable, `0` disables). *Alt:* history only when the user clicks. *Cost:* one clone per repo per day.
24. 🏗 **UUIDv7** primary keys (time-ordered, index-friendly).

## Analysis engine

25. 🏗 **Snapshot via `git clone`, not the REST API.** One clone gives per-commit line stats and file sizes
    that would otherwise cost thousands of rate-limited API calls. Limits: 500 MB repo size, 5 000 most recent
    commits, 5 min clone timeout, files > 2 MB and binaries skipped. The token is passed as an HTTP header via
    `GIT_CONFIG_*` environment variables — never in the command line or `.git/config`; the clone is deleted afterwards.
26. 🧭 **Scoring formulas** (all in `Domain/Analyses/Engine`, each metric stores its 0–100 `points` and `weight`,
    so every score is exactly the weighted sum of stored metrics):
    - *Activity* = 40 % recency (100 at ≤ 7 days → 0 at 365) + 30 % commits/week over 12 weeks (3/week = 100)
      + 30 % share of the last 26 weeks with a commit.
    - *Structure* = 35 % large-file share (25 % of files > 500 LOC ⇒ 0) + 25 % monolith check + 20 % median file
      size (≤ 200 LOC = 100 → 800 = 0) + 20 % folder depth (average 1.5–5 ideal).
    - *Quality* = 30 % tests (any = 60, +40 as test files approach 20 % of source) + 25 % commit-message quality
      + 15 % README + 15 % lint/format config + 15 % CI.
    - *Overall* = 30/30/40 (from the spec).
    Thresholds (500 LOC, 3 commits/week, …) are my judgement calls — the obvious knobs to tune.
27. 🧭 **Commit-message quality lives in the Quality score**; the spec lists it as its own category but only
    scores activity/structure/quality. Commit size, variance and buckets are informational (drive feedback + charts).
28. 🧭 **Monolith = more than half of the code in files over 500 lines.** The spec said "few large files vs many small".
29. 🧭 **Message score:** vague (fix/update/stuff/wip/… or < 8 chars, also `fix: stuff`) = 0; otherwise 60,
    +20 for a 15–72-char subject, +20 for a Conventional Commits prefix. Merge commits are excluded.
30. 🧭 **USER_CONTRIBUTION scope**: commits matched by e-mail (all verified GitHub e-mails + both noreply forms);
    structure/tests measured over the files the user touched; README/lint/CI stay repository-wide.
    *Limitation:* commits made with an unverified e-mail are not attributed (the feedback tells the user this).
31. 🧭 **Vendored/generated code is ignored** (node_modules, dist, bin/obj, migrations, *.min.js, *.g.cs, …).
32. 🧭 **Language distribution uses GitHub's Linguist byte counts** (per repo, aggregated over selected repos),
    not our own file-extension count. *Limitation:* the user-scope view shows repo languages, not the user's own.
33. 🧭 **Timeline** = weekly buckets (Monday, UTC), gap-filled, capped at 104 weeks. **Largest files** = top 10.

## Feedback

34. 🧭 **Rule set** (thresholds are judgement calls, all in `Domain/Analyses/Rules`):
    vague commits ≥ 30 % HIGH, ≥ 10 % MEDIUM, else a strength · very large commits when ≥ 20 % change ≥ 1,000 lines
    (MEDIUM) · dormant after 180 days (MEDIUM) · consistent activity when ≥ 50 % of the last 26 weeks are active
    (strength) · monolith HIGH · files > 500 lines MEDIUM · good separation (strength) · no tests HIGH, < 10 % test
    files MEDIUM, ≥ 20 % a strength · missing README MEDIUM · missing lint / CI LOW · no attributed commits HIGH.
    UC4's "at least two items incl. commit + structure feedback" holds **whenever the scope has commits and source
    files**: those two rules then always emit a problem or a strength. An analysis with no commits (e.g. a
    contribution scope with nothing attributed) instead gets the "no commits" finding explaining why.
35. 🧭 **Strengths are first-class** (`isStrength`), not a fourth severity. The spec only had LOW/MEDIUM/HIGH.
36. 🧭 **AI feedback is implemented but optional** (enabled by `AiFeedback__ApiKey`). Model **`claude-opus-5`**,
    structured JSON output, at most 5 findings, grounded only in the stored metrics/files/rule findings, never
    duplicating rule findings. Server-side refusal fallback (`fallbacks: "default"`) is enabled — tell me if you'd
    rather not use that beta. If the AI call fails, the rule-based analysis still succeeds. *Cost:* one Opus call per
    analysis run (incl. scheduled ones) — consider disabling the schedule or picking a cheaper model if you enable it.

## Product

36a. 🧭 **Portfolio is private until published** (`isPortfolioPublic`, default false) — the spec's UX flow ends
    with "portfolio publish". Owners can preview their unpublished portfolio.
37. 🧭 **Public portfolio shows strengths only**; improvement feedback stays on the private dashboard.
38. 🧭 **Private repositories never appear on the public portfolio**, even when selected; project links to hidden
    repositories are filtered out.
39. 🧭 **Portfolio URL is `/u/{githubLogin}`** (API also accepts the user id, as in the spec).
40. 🧭 **Default selection on import:** selected only if you own it and it is not a fork (org and collaborator
    repositories are imported but opt-in). The spec said "default true"; forks/org repos would pollute the portfolio.
41. 🧭 **Import includes owner, collaborator and organisation repositories.**
42. 🧭 **Private repositories are opt-in at the deployment level** (`GitHub__IncludePrivateRepositories`), because
    GitHub's only scope for private code (`repo`) also grants write access. Default scopes: `read:user user:email`.
43. 🧭 **Projects (the spec's "future" abstraction) are implemented**: name, description, up to 10 https image
    URLs, linked repositories, sort order. Image *upload* is not implemented (URLs only).
44. 🧭 **LinkedIn is a validated https linkedin.com link** — no LinkedIn API integration (per the spec's advice).
45. 🧭 **Not built** (spec backlog, deliberately out of scope): GitLab, recruiter view, benchmarking/leaderboards,
    team analytics, CI coverage import, static-analysis integrations, achievements, public API keys, historical
    score back-fill from commit history.

## Security

46. 🔒 **Session = bearer JWT (7 days) in the SPA's `localStorage`**, obtained by exchanging a single-use,
    60-second sign-in code (`POST /api/auth/exchange`) that the OAuth callback appends to the SPA URL.
    *Originally* an HttpOnly SameSite=Strict cookie — dropped when you chose GitHub Pages + a separately hosted
    API, because cross-site cookies are blocked by browsers. *Trade-off:* a token in `localStorage` is readable by
    an XSS; mitigated by a strict CSP (no inline scripts, `script-src 'self'`) on both hosts. Tokens are not
    server-revocable (add a denylist if needed). CORS allows only the configured frontend origin.
47. 🔒 **OAuth `state`** is random, stored encrypted (Data Protection, 10-min expiry) in a SameSite=Lax cookie and
    compared in constant time; `returnUrl` is restricted to local paths (open-redirect guard). Tested.
48. 🔒 **GitHub tokens encrypted at rest** with ASP.NET Core Data Protection; the key ring is stored in PostgreSQL.
    ⚠️ *Trade-off:* the key ring itself is **not** encrypted and sits in the same database as the tokens, so a full
    database leak exposes both. Upgrade path: `ProtectKeysWithAzureKeyVault` (Key Vault + managed identity).
49. 🔒 **Other users' resources return 404, never 403** (no ID probing). Tested.
50. 🔒 **Rate limits** per user (or IP when anonymous) per minute: 30 analysis requests, 5 imports, 20 sign-in
    code exchanges. Also: the dashboard shows the 8 most severe improvement items; the runs list returns the latest 50.
51. 🔒 **Security headers + CSP** (`script-src 'self'`, no external origins; `/scalar` exempt because it loads
    from a CDN). Forwarded headers are trusted from any proxy (container platforms), so cookies
    get `Secure` behind TLS termination.

## Tooling & verification

52. ⚙️ **Lint = `dotnet format --verify-no-changes`**; the build runs with warnings-as-errors, code-style
    enforcement and NuGet audit. Naming: `_camelCase` private fields, PascalCase constants.
53. ⚠️ **Docker Desktop was broken on this machine** (engine returns HTTP 500), so:
    - the integration tests were verified against a **portable PostgreSQL 17** started from the scratch
      directory (`DEVINSIGHT_TEST_POSTGRES` env var — the fixture supports both); in CI they use Testcontainers,
      and CI **fails** (not skips) if Docker is missing;
    - the **Dockerfile and docker-compose.yml are unverified** — please run `docker compose up --build` once.
54. ⚙️ **API integration tests use fakes for GitHub and git** (every OAuth code is a login; every user owns two
    repos) and the real database, auth, background worker and HTTP pipeline.

## Hosting (after you chose "GitHub Pages + hosted backend", then Azure)

55. 🧰 **API on Azure Container Apps (consumption, scale 0–1)**. *Alt:* App Service F1 (60 CPU-min/day is too
    little for git clones), AKS (overkill), Cloudflare Workers / Supabase functions (no .NET, no git).
    Max one replica because sign-in codes and the analysis queue are in memory.
56. 🧰 **Image in GitHub Container Registry** (free, public package). *Alt:* Azure Container Registry (~USD 5/month).
57. 🧰 **Infrastructure as Bicep** (`infra/main.bicep`), secrets passed as `@secure()` parameters from a local,
    git-ignored `.env.azure`, stored as Container Apps secrets. Compiled in CI (`infra.yml`).
58. 🔒 **GitHub Actions → Azure via OIDC federation**, scoped to the `production` environment and Contributor on
    one resource group — no Azure secret in GitHub.
59. 🧭 **Database is your choice at setup time**: Azure PostgreSQL Flexible Server B1ms (free 12 months, then
    ~USD 13/month) or an external Supabase/Neon connection string.
60. 🧭 **SPA reads the API URL at runtime** (`config.json`, written by the Pages workflow from the
    `DEVINSIGHT_API_URL` repository variable) instead of baking it in at build time.
61. ⚠️ **Scheduled re-analysis mostly won't fire** when the app scales to zero; history grows when the app is used.
    Options if you want daily snapshots: `minReplicas: 1` (costs money) or an Azure Container Apps *job* on a cron.
62. ⚠️ **Not verified end to end**: the Bicep template, workflows and cross-origin sign-in have been written,
    compiled/type-checked and unit/integration-tested, but not run against real Azure/GitHub Pages — that happens
    when you run `scripts/setup-azure.sh`. Your bio and LinkedIn URL (from cmaintz-site) go in via Settings then.

## Frontend

Every frontend decision (libraries, patterns, UX, charts, CSP and cross-origin auth handling, API assumptions)
is in [`DECISIONS-frontend.md`](DECISIONS-frontend.md), written by the agent that built it. Highlights to review:
score bands (≥75 Strong / ≥50 Fair), per-repo "Analyse" starts both scopes, ngx-charts with text summaries and
data-table twins for every chart, token cleared on a 401 from the session probe.

## Foundry

63. ⚙️ **Reusable workflows pinned to Foundry `v2.3.0`** (by commit SHA) — the first release with the .NET stack
    and the fixes for the security-facade startup failure and the ratchet report on a new baseline.
64. ⚙️ **Two packages, one oracle.** `backend/mise.toml` and `frontend/mise.toml` each define the six verbs;
    the root `mise.toml` runs each verb in both. CI calls the gate facade once per package
    (`stack: dotnet` / `stack: ts`). Toolchains are pinned per package, so the backend job doesn't install Node.
65. ⚙️ **Frontend verbs delegate to npm scripts** (`npm run lint` = `ng lint` + Prettier, `npm test` =
    `ng test` with coverage thresholds) rather than calling eslint/vitest directly as Foundry's TS template does —
    Angular's builders own those invocations.
66. ⚙️ **Backend coverage floor = 85 % merged line coverage** (measured 91.3 % with all tests; first set at 75 %) of `DevInsight.*` (migrations excluded).
    coverlet.MTP has no threshold option yet, so the `test` verb merges the Cobertura reports with ReportGenerator
    and checks the floor itself. Measured: 59.6 % *without* the PostgreSQL integration tests (API layer 0 %) —
    the floor assumes they run, which CI guarantees and local runs need Docker for.
67. ⚙️ **habit-hooks**: backend uses the generic preset (file length) with migrations excluded — currently clean.
    Frontend uses the TypeScript preset; its sensors (knip, ts-morph, jscpd) became pinned devDependencies.
    Their first run reported 190 findings in the new frontend (unused exports, test-only code, duplication,
    comment noise); these are being **fixed rather than baselined** since the code is brand new.
68. ⚙️ **Foundry bug found:** `foundry-init.sh` generates `concurrency: { group: gate-${{ github.ref }}, … }`.
    `${{ }}` inside a YAML flow mapping is invalid per the YAML spec (PyYAML rejects it); ours uses block style.
    Not verified whether GitHub's parser tolerates it — worth fixing upstream either way.
69. ⚙️ **`AGENTS.md` (canonical) + `CLAUDE.md` (`@AGENTS.md`)**, per Foundry's convention: only what an agent
    can't discover from the repo — the oracle, the invariants, the decision-log rule.
70. ⚙️ **foundry-init side effect:** it set the *global* mise setting `windows_default_inline_shell_args = bash -c`
    on your machine (Foundry's intended Windows setup).

## Added after the code review

71. 🧰 **Container size 1 vCPU / 2 GiB with one analysis at a time** (`AnalysisWorker__MaxConcurrency=1` in Azure).
    *Alt:* 0.5 vCPU / 1 GiB with two concurrent runs — too tight for two 500 MB clones plus 5,000-commit logs.
72. 🔒 **PostgreSQL firewall allows "Azure services" (0.0.0.0)** because Container Apps on the consumption plan has
    no fixed outbound IP. That admits any Azure-hosted client, *including other tenants*, to attempt a login — the
    strong generated password is the only barrier. *Upgrade:* VNet-integrated Container Apps environment + private
    PostgreSQL (costs more), or Microsoft Entra authentication. Supabase/Neon have the same property (internet-reachable).
73. 🧰 **Azure extras:** Log Analytics (PerGB2018, 30-day retention) for container logs; PostgreSQL 32 GB storage,
    7-day backups, no HA/geo-backup; liveness probe on `/health`; every deploy pushes the commit-SHA tag **and**
    `:latest` — the rollout uses the SHA, `:latest` only seeds the first infrastructure deploy.
74. ⚙️ **Deploy workflows skip (with a notice) until Azure is configured**, instead of failing red on the first
    push to `main`.
75. ⚙️ **Foundry extras copied in with the scaffold:** `renovate.json` (Renovate keeps pinned actions, mise
    tools and packages current — needs the Renovate GitHub app installed to do anything); `bootstrap.yml` runs
    daily at 06:00 UTC and opens a PR only when the habit-hooks baseline can shrink; `scripts/foundry-verb-wrap`
    records per-verb timings in the git-ignored `.foundry/telemetry.jsonl` (`scripts/foundry-loop-report` reads it);
    `scripts/ruleset_guard.py` backs the security workflow's ruleset-guard, whose gate-defining paths are listed in
    `security.yml` (mise files, `.editorconfig`, `Directory.Build.props`, lint/format/habit-hooks configs,
    `angular.json`, the Foundry workflows).
76. ⚙️ **Frontend audit fails on any advisory** (`npm audit --audit-level=low`), matching the backend. There were
    none at any level when this was set.
77. 🔒 **Known, accepted:** the one-time sign-in code is not bound to the browser that started the OAuth round trip,
    so an attacker could plant *their own* 60-second code on a victim ("login CSRF" — the victim would be signed in
    as the attacker). Low impact here (no payment or private data flows into the attacker's account). *Fix if
    needed:* issue a nonce with the login redirect, keep it in `sessionStorage`, and require it at `/exchange`.
78. 🧰 **PostgreSQL ARM API `2025-08-01`** — the first stable version whose schema lists PostgreSQL 17.

## Code-size audit (refactor/code-audit)

79. ⚙️ **Function ≤ 18 code lines, file ≤ 300 lines — enforced, for production *and* test code.** Backend:
    `CodeSizeTests` parses every `.cs` file with Roslyn (migrations exempt); frontend: `check-function-length.mjs`
    in `npm run lint` (`describe` blocks exempt, `it` bodies measured). "Code line" = not blank, not a comment,
    not only brackets/punctuation. *Alt:* ESLint `max-lines-per-function` alone — counts raw lines, and there is
    no C# equivalent in the analyzers.
80. 🏗 **Shared concepts extracted in the backend:** `CurrentUser` (bound into endpoint handlers instead of every
    handler reading claims), `IUserOwned` + `OwnedBy(...)` (one ownership/404 rule for repositories, projects and
    runs), `RepositoryInsights` (the latest-analyses + history loading the dashboard and portfolio shared), and the
    analysers split into *facts* (measurements) and *metrics* (scoring).
81. ⚙️ **Test names describe one behaviour**; tests that asserted two ("…and…", "…but…") were split.
