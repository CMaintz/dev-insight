# Frontend decision log

Each bullet: the choice → the alternative rejected, and why.

## Toolchain & dependencies

- **Angular 22.2.0 CLI scaffold** (standalone, strict, SCSS, zoneless, Vitest, no SSR, 2025 file-name style: `dashboard.page.ts`, not `dashboard.component.ts`). Rejected the 2016 `*.component.ts` style: the CLI now defaults to 2025 and names stay shorter.
- **All versions pinned exactly** (a script rewrote every `^`/`~` from `node_modules`, including the ones `ng new` and `ng add angular-eslint` added). Rejected ranges because the task requires reproducible pins. The lockfile is committed as-is.
- **`@swimlane/ngx-charts` 25.0.2** (the spec asked for it) with its peers `@angular/animations`, `@angular/cdk` and `@angular/platform-browser-dynamic`, all 22.2.0. There is no other UI kit: rejected Angular Material because the brief says "prefer CDK only". CDK is installed only as an ngx-charts peer.
- **`provideAnimationsAsync()`** in app config. ngx-charts still uses `@angular/animations`, and the async provider keeps the engine out of the initial bundle. Rejected `provideNoopAnimations` because it kills chart transitions for everyone. Motion is disabled per chart when `prefers-reduced-motion` is set.
- **angular-eslint 22.5.0 flat config**, added by `ng add` and extended with: OnPush required, `prefer-inject`, `prefer-signals`, `max-lines: 300` (mirrors the Foundry structural-smell gate), `max-lines-per-function: 25` (off in specs; see the function-size section), `eqeqeq`, `no-console` (except `console.error` in `main.ts`), template control-flow and self-closing rules, and a11y template rules. Rejected the bare recommended set because it enforces none of the task's conventions.
- **`npm run lint` = `ng lint --max-warnings=0 && prettier --check .`**, which makes warnings fail the gate. Prettier has `endOfLine: lf`, and `.prettierignore` covers dist, coverage and the lockfile.
- **Coverage thresholds: statements 90 / branches 80 / functions 85 / lines 90**, set in `angular.json` `test.options` so `npm test` is just `ng test --watch=false`. The measured result is 95.9 / 90.1 / 94.1 / 97.1. Rejected the suggested 70% because the suite comfortably meets a stricter bar. `src/testing/**` (fixtures/harness) and `main.ts` are excluded, but no app source is.
- **Budgets left at CLI defaults** (initial 500 kB warn, component style 4 kB warn). Every route is lazy and ngx-charts/d3 only load with chart pages. The initial bundle is 335 kB raw / 94 kB transfer.

## CSP compliance (coordinator requirement)

- **The theme pre-paint script is an external file (`public/theme-init.js`)**, not inline. The backend CSP is `script-src 'self'`, which blocks inline scripts. The synchronous external script still runs before first paint, so there is no light/dark flash.
- **Critical-CSS inlining is disabled** (`optimization.styles.inlineCritical: false` in the production config). Beasties injects an inline `onload` script that swaps `media="print"` to `media="all"`. The CSP would block it, so the stylesheet would never apply. Verified: `grep -c "<script>" dist/devinsight/browser/index.html` = 0.
- **No external origins**: system font stack, no web fonts, no CDNs. Images come only from GitHub avatars and user-supplied https project images (`img-src https:`).

## Cross-origin deployment (GitHub Pages SPA + Azure API)

- **Runtime config `public/config.json` (`{ "apiBaseUrl": "" }`)** is loaded by `provideAppInitializer` before bootstrap completes. It uses `fetch` relative to `document.baseURI`, so it resolves to `/dev-insight/config.json` on Pages, with `cache: no-store`. Any failure (missing file, HTTP error, bad JSON, missing or non-string `apiBaseUrl`, or a non-empty value that is not an http(s) URL) shows a full-page "Couldn't load app configuration" screen with a Reload button, and nothing calls the API. `""` stays a valid explicit same-origin value. Rejected silently falling back to same origin, because on Pages that sends API calls to the static host. The check applies in every build mode, since the dev server serves `config.json` too. Rejected build-time `environment.ts` because the Pages deploy must inject the API URL without a rebuild. Rejected `HttpClient` for the fetch because it would recurse through the interceptors.
- **Interceptor chain `[error, bearer, apiBaseUrl]`:**
  - The bearer interceptor only matches our relative `/api/…` and `/health` paths, and runs _before_ the base URL is applied, so the token can never reach a third-party URL (e.g. image hosts).
  - It never overrides an explicit `Authorization` header.
  - The base-URL interceptor handles trailing slashes, and `/healthy` is not treated as `/health`.
  - The error interceptor still sees the relative URL for its exemption checks.
- **`AuthTokenStore`** keeps the token under the localStorage key `devinsight.token`, with try/catch and an in-memory fallback. It stores the token string only, not `expiresAt`: an expired token surfaces as a 401 and is cleared. Rejected sessionStorage because sign-in would not survive a new tab. The localStorage XSS exposure is accepted and mitigated by the strict CSP (no inline scripts, `script-src 'self'`).
- **The token is cleared on 401**: in the interceptor for non-exempt requests, and additionally in `SessionStore` when the `/api/me` probe itself returns 401. That second case is inside the interceptor exemption, but a token the API rejects is stale, and keeping it would re-send it forever. Other probe failures (5xx, network) keep the token. Each `/api/me` probe has a generation number, and any newer state change (refresh, `setProfile`, `markSignedOut`) bumps it. A stale probe, such as the boot probe answering 401 after the OAuth callback stored a fresh token, therefore cannot clear the token or overwrite the session.
- **Logout** is `POST /api/auth/logout` (errors ignored, it is stateless now), then clear token → mark signed out → navigate to `/`.
- **Sign-in** does a full-page navigation to `${apiBaseUrl}/api/auth/github/login?returnUrl=<local path>`.
- **Callback route `auth/callback`** (lazy, public) works like this:
  - It reads `code` / `returnUrl` / `error` query params via component input binding.
  - It POSTs `/api/auth/exchange` `{ code }` with the `SILENT_ERRORS` context, because the page renders its own error rather than a toast.
  - It stores `accessToken`, calls `SessionStore.refresh()`, then `navigateByUrl(safeReturnUrl(returnUrl), { replaceUrl: true })`. `replaceUrl` keeps the one-time code out of browser history.
  - Failures (`?error=`, missing code, failed exchange) show "Sign-in failed" with a **Try again** button (restarts OAuth with the same return path) and a home link.
- **`safeReturnUrl()`** accepts only paths starting with a single `/`. It rejects `//host`, `/\host` and absolute URLs, falling back to `/dashboard`. It is shared by sign-in and the callback.
- **GitHub Pages build:**
  - `angular.json` configuration `pages` sets only `baseHref: "/dev-insight/"`. Angular configs cannot "extend", so `build:pages` runs `ng build --configuration production,pages` to stack them.
  - `scripts/spa-fallback.mjs` then copies `index.html` to an identical `404.html`, because Pages has no SPA fallback and deep links and `/dev-insight/auth/callback` would otherwise 404.
  - The same script writes `.nojekyll`, so Jekyll never hides underscore-prefixed files.
  - The script is tested with `node --test`, which is chained into `npm test`.
- **Base-href safety:**
  - All asset references are relative (`theme-init.js`, `favicon.ico`, `config.json`), and router links go through the base href.
  - Bare `href="#id"` links resolve against `<base>` and would navigate to the landing page. The skip link and the landing "How it works" link now use `jumpToFragment()`, which focuses and scrolls to the target instead.
- **CSP `<meta http-equiv>`** in `index.html` (Pages cannot send headers):
  - The exact policy is the coordinator's.
  - `connect-src 'self' https:` is needed because the API origin is runtime config.
  - Rejected pinning the API origin because the value is only known at deploy time.
  - The meta tag also applies when the backend serves the SPA; it combines with the header policy, and both allow everything the app loads.
  - Verified: no inline `<script>` in the production, Pages or dev-server HTML.

## Architecture & patterns

- **API clients: one small `@Injectable` per resource** (`ProfileApi`, `ReposApi`, `AnalysisApi`, `DashboardApi`, `ProjectsApi`, `PortfolioApi`). GETs are exposed as `httpResource` factories (called from component field initialisers, so the resource lives and dies with the page). Mutations return Observables that are awaited with `firstValueFrom`. Rejected one god-service, and rejected NgRx as over-engineering at this size.
- **`safeValue()` / `stickyValue()` helpers.** In Angular 22, `resource.value()` throws in error state, so templates read a guarded computed. The dashboard keeps its last value while a scope change refetches, so the page dims instead of flashing a skeleton (dataviz anti-pattern).
- **`SessionStore`** is the single cached `/api/me` probe with a shared in-flight promise. The guard, header, landing and settings all read it. Rejected per-component probes, which fire several requests on first load.
- **Error interceptor**: a 401 clears the session and navigates to `/`, except for exactly `/api/me` and the prefix `/api/portfolio/`. A test caught that a naive prefix match would also have exempted `/api/me/profile`. 404 is left to pages ("not analysed yet" / "portfolio not found"). Everything else becomes a toast built from problem details. The `SILENT_ERRORS` `HttpContextToken` lets a caller opt out.
- **`BROWSER_LOCATION` injection token** for the full-page OAuth redirect, so sign-in is unit-testable. The return URL is forced to a local path (`/…`, not `//…`), matching the backend rule.
- **Run polling: `RunPoller`** uses `timer(2s, 2s)` + `exhaustMap` (a slow response never overlaps the next poll) + `takeWhile(inclusive)`, with a 300-poll cap (~10 min). The interval and cap are plain module constants. They used to be injection tokens overridden only by tests; the Foundry gate flagged that as test-only code, so the spec now drives the real 2 s / 300-poll values with fake timers.
- **`RunTracker` is an app-wide root store of runs.** Polling continues if the user navigates away. Its `settledCount` signal bumps when a run finishes, and pages reload through `reloadOn()`. Rejected per-component polling because leaving the page would orphan the run and lose its state.
  - Poll subscriptions are kept per run. Re-analysing a repository cancels its old polls, so late updates can't bring forgotten runs back or double-count `settledCount`. `SessionStore.markSignedOut()` (logout or a 401) calls `RunTracker.stopAll()`, so no poll keeps hitting the API without a token.
  - When the 300-poll cap or a polling error ends tracking before a final status, the run becomes a terminal "Timed out — refresh later" state (with a toast) instead of staying active forever.
  - Per-repo "Analyse" uses `Promise.allSettled`: if one scope's POST fails, the scope that started is still tracked, and the failure is rethrown to the caller's error handling.
- **Per-repo "Analyse" starts two runs (`scope=repo` and `scope=user`)**, and progress reads "Analysing (1/2)". Rejected analysing only the currently toggled scope: the toggle would otherwise land on "not analysed yet" right after an analysis, and `run-all` already does both scopes.
- **`ScopePreference` is a root signal shared by the dashboard and repository detail**, so the scope survives navigation (not persisted across reloads).
- **`WorkspaceActions`** (import / analyse-all) is shared by the dashboard and repositories pages, with an `importVersion` signal to refetch after an import.
- **Route params use `withComponentInputBinding()`** (`id`, `handle` as `input.required`). Rejected `ActivatedRoute` subscriptions as less signal-friendly.
- **`withViewTransitions()` and `withInMemoryScrolling({ scrollPositionRestoration: 'top' })`**: cheap polish.
- **List pages gate their skeleton on `resource.status() === 'loading'`**, not on `isLoading() && !hasValue()`. A `defaultValue: []` resource reports `hasValue()` true while loading, which would flash the empty state. The `reloading` status after an import keeps the list visible. Covered by a skeleton-before-flush spec.

## UX choices I invented

- **Score bands: ≥75 "Strong" (status-good), ≥50 "Fair" (status-warning), <50 "Needs work" (status-critical)**, always shown with a text label and never by colour alone. The thresholds are mine, since the API defines only 0–100.
- **The dashboard shows the first-run guide** (import → select → analyse → view → publish) until something is analysed _and_ the portfolio is published. The next step is highlighted with a direct CTA.
- **"Vague commits" is a KPI tile**, not a chart, because a single share reads best as a number (dataviz "is it even a chart?").
- **Repositories list uses a responsive row layout instead of a wide table**, because it has to fit 360 px phones. Selection toggles optimistically and rolls back on error.
- **Project delete uses an inline two-click "Delete → Confirm delete"** instead of `window.confirm`, which is untestable and inaccessible-ish. Delete is disabled on every card while the form is open.
- **The project form is keyed by its target** (`@for … track target.key`, with key = project id or `new`), so choosing Edit on another card rebuilds the form with that project's values. Previously the form kept the first project's values but saved to the second. Rejected disabling the other cards' Edit buttons as the only fix, because switching the edit target directly is a reasonable action.
- **Project images are capped at 10 URLs.** Sort order defaults to max + 1, and multi-select is an accessible checkbox fieldset. Rejected `<select multiple>` because it is hard to use on touch.
- **Form limits match the backend (coordinator request):** bio max 2000, project name max 200, project description max 4000, images max 10. These were originally 500 / 100 / 2000 / 6, my own UI-side guesses before the backend limits were known. An over-long description and a whitespace-only name are rejected inline (`notBlankValidator`).
- **Dimension weights (activity 30 %, structure 30 %, quality 40 %)** live in one constant, `DIMENSION_WEIGHTS` in `shared/format/metrics.ts`, used by the landing page and `ScoreBreakdown`. It mirrors the backend's `Scoring.cs` and must change with it.
- **Toasts** auto-dismiss after 6 s and at most 4 are visible (the oldest is dropped).
- **Number and date formatting uses a hard-coded `en` locale** (dates in UTC). Rejected following the browser locale for now: the UI copy is English-only, and a mixed-locale page reads worse.
- **Portfolio repository cards show the top 3 languages by share**, falling back to the primary `language` when the byte map is empty.
- **Screen-reader status for runs:** `role="status"` sits on a wrapper around the run state, so "View results" stays a link for assistive tech.
- **LinkedIn validator accepts `https://linkedin.com/...` and any `*.linkedin.com` subdomain** (e.g. `www.`, `uk.`). It rejects `http:` and look-alikes such as `linkedin.com.evil.io`.
- **The portfolio page is a hero** (avatar or initials fallback, bio, GitHub/LinkedIn), plus score rings, highlight tiles (repos, commits, active weeks, main language), activity, score evolution, languages, strengths, project cards with a screenshot switcher, and repo cards with per-dimension badges. An owner viewing an unpublished portfolio gets a "Preview" banner linking to settings.
- **Theme** follows `prefers-color-scheme` by default. The header toggle stamps `data-theme` on `<html>` and persists it to `localStorage` (wrapped in try/catch). Dark tokens are applied under both `@media (prefers-color-scheme: dark) :root:not([data-theme=light])` and `:root[data-theme=dark]`.
- **Muted text uses `#6b6a66` light / `#a3a29a` dark**, not the dataviz reference `#898781`, so small text meets WCAG AA (≥4.5:1) on the surfaces.

## Charts (dataviz skill)

- **Palette: the dataviz reference categorical palette**, validated with `validate_palette.js` in both modes. Light: all checks PASS, worst adjacent CVD ΔE 9.1, with a contrast WARN on slots 3–5 that is relieved by a table view on every chart. Dark: all PASS, CVD ΔE 8.4. Slots follow the entity in fixed order (Overall, Activity, Structure, Quality), never rank.
- **Forms**: commits/week and score evolution are line charts on a time axis (0–100 fixed axis for scores, so no dual axis). Languages are horizontal percentage bars, top 6 + "Other". Rejected a pie/donut because close shares are hard to compare. Commit sizes are single-colour ordinal columns.
- **Every chart is a `<figure>`** with a heading, a plain-language summary (e.g. "Overall score rose by 12 to 62 …"), the plot marked `aria-hidden`, and a "Show data table" `<details>` twin. Empty states replace the plot with text.
- **Score evolution needs ≥2 points to draw.** With one snapshot the summary text is shown instead, since a one-point line reads as broken.
- **Chart chrome is themed through global CSS** (`_charts.scss`: hairline solid gridlines, muted axis text, themed tooltip). ngx-charts takes JS colour arrays, so wrappers pick light/dark arrays from `ThemeService.effective()`.

## Foundry structural-smell gate (`habit-hooks --all`)

- **`knip.json`**:
  - Production entries are `src/main.ts!` and `scripts/*.mjs!`. Specs, `scripts/*.test.mjs` and `src/testing/**` are test-only (excluded from the production project).
  - Rejected listing flagged files as entries: that would hide test-only code instead of removing it.
- **`@angular-eslint/builder` is now an explicit pinned devDependency** because `angular.json` references it directly. Rejected leaving it to arrive transitively through `angular-eslint`.
- **`.jscpd.json`** scans only `src` and `scripts`, excluding specs and the test harness. Without it, jscpd was reporting clones inside `package-lock.json`.
- **Test-only exports removed, not whitelisted.** Specs now go through each module's real entry point instead of reaching into its internals:
  - `summariseRuns` via `RunTracker.stateFor`
  - `guideSteps` via the rendered first-run guide
  - `splitFeedback` via the `FeedbackList` component
  - URL/path helpers via `AppConfig` and the interceptors
  - `isHttpsUrl` / `isLinkedInUrl` via the form validators
  - `gitHubLoginUrl` via `SessionStore.signIn`
  - storage keys as literals in the specs
  - Dead code deleted: `toScopeParam`, `formatShortDate`.
- **Named abstractions for duplicated blocks**:
  - `WorkspaceActionsBar` (Import / Analyse-all buttons, shared by the dashboard and repositories pages).
  - `ScoreBreakdown` (overall ring + three weighted dimension rings, used by the dashboard, repository detail and portfolio hero).
  - `RepoFacts` (stars / forks / last-activity list, with projected language chips).
  - `control-surface` SCSS mixin (shared by buttons and text inputs).
  - `browser-storage.ts` (`readStoredValue` / `storeValueIfPossible`, shared by the theme and token stores).
- **User-action errors go through `UserActionErrors`** (`core/http/surfaced-errors.ts`), not empty `catch {}` blocks:
  - It swallows only `HttpErrorResponse`s the interceptor has already shown as a toast (everything except 401 and 404).
  - A 404 gets a caller-specific toast, e.g. "That repository no longer exists — re-import from GitHub."
  - 401 and non-HTTP errors are rethrown.
  - Callers branch on `undefined`.
  - Logout ignores its own errors explicitly with `catchError`, because the token must be cleared regardless.
- **No comments in production `.ts`.** The sensor flags every comment regardless of intent. Explanations were moved into names (`CVD_VALIDATED_PALETTE`, `SCORE_SERIES_IN_COLOUR_SLOT_ORDER`, `dropTokenIfRejected`, `UserActionErrors`) or already live in this log (CSP, base-href fragment links, interceptor order, palette validation numbers).
  - The comments in `app.config.ts` (config before the first request, interceptor order, async animations) were not flagged by the sensor and are kept, because they explain a _why_.
  - SCSS/HTML files are outside the sensor's scope and keep their short why-comments.

## Function size and duplication (owner standard: ≤ 18 code lines per function, ≤ 300 lines per file)

- **`scripts/check-function-length.mjs` enforces the limits and runs as part of `npm run lint`** (and so `mise run lint` / `gate`). It is covered by a `node --test` suite that runs in `npm test`.
  - It uses ts-morph over `src/**/*.ts` and `scripts/*.mjs`.
  - A "code line" is non-blank, not a comment, and not bracket/punctuation-only.
  - `describe()` callbacks are skipped because they are containers; `it()` bodies are measured.
  - Nested functions count toward their parent too, which errs on the strict side.
  - Rejected relying on ESLint alone: `max-lines-per-function` counts signature and brace lines and can't skip `describe`.
- **ESLint `max-lines-per-function` is tightened from 60 to 25** raw lines (blank and comment lines skipped) for production code. That is roughly 18 code lines plus signature and closing lines, and gives editor feedback; the script is the authoritative check.
- **Test code follows the same limit through named steps rather than split lines:**
  - Fixtures are composed of smaller builders (`aMetric`, `anInformationalMetric`, `aWeek`, `aPortfolioOwner`/`Repository`/`Project`).
  - The page harness gained `openPage(component, plannedResponses)`, `respond()`, `click()`, `fieldValue()`, `typeInto()`, `requireElement()` and `NOT_FOUND`.
  - `answerRunStarts()` answers the two per-scope analysis POSTs.
  - Specs define page-level steps (`openDashboard`, `openDetail`, `openNewProjectForm`, `addImage`, `editCard`, `search`, `chooseLanguage`).
  - Multi-behaviour tests were split into one behaviour each.
- **The first-run guide is data-driven:** a `GUIDE_STEPS` table of definitions, each with an `isDone(progress)` predicate, is mapped into view steps. Previously this was one long literal function.
- **Named UI pieces replace repeated markup:**
  - `ImportPrompt` — the "no repositories" empty state with the Import button, on the dashboard and repositories pages.
  - `LoadError` — "Could not load …" + Retry, on the dashboard, repositories, projects and portfolio.
  - `PageSkeleton` — the loading layout on the dashboard and portfolio.
- **SCSS mixins in `src/styles/_mixins.scss`**, reachable as `@use 'mixins'` via `stylePreprocessorOptions.includePaths`:
  - `panel` — bordered surface card, used by `.card`, `.table-wrap`, portfolio cards and strengths, the repositories list.
  - `pill` — used by `.chip` and feedback tags.
  - `control-surface` — buttons and inputs.
  - Judged coincidental and left alone: `kpi-tile` and `empty-state` (different padding / a dashed border) and `chart-frame` (not a panel).
- **Repeated production logic extracted:**
  - `BusyFlag` — "run once at a time while flagged busy", used by import, analyse-all, and the project and profile saves. It also resets on a thrown error; before, a rethrown 401 could leave the flag stuck on.
  - `WorkspaceActions.analyseRepository()` — one place for "analyse this repo and explain a 404", used by the repositories and detail pages.
  - `listOrEmpty(resource)` — array resources read as `[]` until loaded or on error.
  - `parseApiDate()` — shared by date formatting and the chart mappers.
  - `RunTracker.observeUntilSettled()` — the poll observer, extracted from `track()`.
- **Deliberately left:** the remaining `hasValue() ? value() : undefined` inside `safeValue`/`stickyValue`, since they are the helpers themselves.

## Testing

- **Page specs render the real ngx-charts under jsdom.** They work: ngx-charts falls back to a 600×400 viewport. I initially stubbed the charts, but `overrideComponent` forced JIT template compilation and hid template coverage, so I dropped the stubs.
- **The page harness settles with `TestBed.tick()` + macrotask turns instead of `whenStable()`**, because a pending `HttpTestingController` request keeps the app unstable and `whenStable()` would hang.

## Assumptions about the API (not covered by docs/API.md)

- **`POST /api/auth/exchange`** takes JSON `{ code }` and returns 200 `{ accessToken, expiresAt }` (same shape as `/api/auth/token`). Any non-2xx is treated as "expired or invalid code".
- **The API redirects to `<frontend base>auth/callback?code=…&returnUrl=…`** or `?error=signin_failed`. `returnUrl` is an app-relative path such as `/dashboard` (without `/DevInsight`), and the router resolves it under the base href.
- **The API sends CORS headers** allowing the Pages origin and the `Authorization` header. No credentials/cookies are needed.

- **`Metric.weight` is a 0–1 fraction** and is displayed as a percentage.
- **`commitQuality` metrics** have no dimension-score chip, since the API exposes only activity/structure/quality scores.
- **Metric keys** come from `backend/.../MetricKeys.cs`. Unknown keys are humanised (`snake_case` → "Snake case"). Values are formatted by key convention: `has_*`/`is_*` → Yes/No, `*_ratio*` → %.
- **`POST /api/repos/import` and `run-all` bodies are empty (`null`).**
- **`GET /api/analysis/{repoId}/history`** is assumed to return 200 `[]`, not 404, when no history exists; either way it degrades to an empty chart.
- **`Portfolio.activity`** is the owner's own-contribution activity (labelled so), and `portfolioPath` is an SPA route used directly with `routerLink`.
- **The dev server proxies `/api` and `/health` to `http://localhost:5080`** (`proxy.conf.json`). Verified the proxy is wired: with no backend running it returns 502.
