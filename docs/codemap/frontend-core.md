---
sha: d4240bcfccce19669016317887401a47a79c4403
slice: frontend-core
kind: domain
paths: [frontend/src/app/core/api, frontend/src/app/core/config, frontend/src/app/core/http, frontend/src/app/core/layout, frontend/src/app/core/models, frontend/src/app/core/notifications, frontend/src/app/core/scope, frontend/src/app/core/theme, frontend/src/app/app.config.ts, frontend/src/app/app.routes.ts, frontend/src/app/app.ts, frontend/src/index.html, frontend/src/main.ts, frontend/src/styles.scss]
---

## Purpose
The Angular SPA shell: it bootstraps the standalone app (`frontend/src/main.ts`), loads runtime config before any HTTP call (`frontend/src/app/app.config.ts#appConfig`), defines the lazy-loaded route table (`frontend/src/app/app.routes.ts#routes`), and provides the typed API clients, HTTP interceptors, toasts, theme and scope state that the feature pages consume (`frontend/src/app/core/api/`, `frontend/src/app/core/http/`).

## Features
- Runtime API base URL read from `config.json` relative to `document.baseURI`, validated as empty or an http(s) URL (`frontend/src/app/core/config/app-config.ts#loadAppConfig`, `frontend/src/app/core/config/app-config.ts#parseConfig`); the deploy writes this file (`.github/workflows/deploy-pages.yml`), and the repo ships an empty default (`frontend/public/config.json`).
- Full-page "Couldn't load app configuration" screen with a Reload button when config fails (`frontend/src/app/app.ts#App`, `frontend/src/app/app.ts#App.reload`).
- Route table: public landing, `auth/callback` and `u/:handle` portfolio; `dashboard`, `repositories`, `repositories/:id`, `projects`, `settings` behind `authGuard`; a `**` not-found page; all lazy via `loadComponent` (`frontend/src/app/app.routes.ts#routes`).
- Typed API clients over `HttpClient`/`httpResource` for analysis, auth exchange, dashboard, portfolio, profile, projects and repos (`frontend/src/app/core/api/analysis.api.ts#AnalysisApi`, `frontend/src/app/core/api/auth.api.ts#AuthApi`, `frontend/src/app/core/api/dashboard.api.ts#DashboardApi`, `frontend/src/app/core/api/portfolio.api.ts#PortfolioApi`, `frontend/src/app/core/api/profile.api.ts#ProfileApi`, `frontend/src/app/core/api/projects.api.ts#ProjectsApi`, `frontend/src/app/core/api/repos.api.ts#ReposApi`), with request/response shapes in `frontend/src/app/core/models/api.models.ts`.
- Bearer token and base URL attached only to `/api/` and `/health` paths (`frontend/src/app/core/http/api.interceptors.ts#isApiPath`).
- Global error handling: 401 signs out, other non-404 errors become error toasts unless the request opts out via `SILENT_ERRORS` (`frontend/src/app/core/http/error.interceptor.ts#errorInterceptor`, `frontend/src/app/core/http/error.interceptor.ts#SILENT_ERRORS`); ProblemDetails bodies or per-status fallback titles supply the text (`frontend/src/app/core/http/problem-details.ts#describeHttpError`, `frontend/src/app/core/http/problem-details.ts#FALLBACK_TITLES`).
- Caller-side helper that toasts a 404 with a caller-supplied message and swallows errors the interceptor already toasted (`frontend/src/app/core/http/surfaced-errors.ts#UserActionErrors.handle`, `frontend/src/app/core/http/surfaced-errors.ts#UserActionErrors.resultOrNothing`).
- Header with skip link, nav links shown only when authenticated, theme toggle, and sign in / sign out (`frontend/src/app/core/layout/app-header.ts#AppHeader`, `frontend/src/app/core/layout/app-header.html`).
- Toast notifications: error/success/info, at most 4 visible, auto-dismissed after 6 s, rendered in an `aria-live` region (`frontend/src/app/core/notifications/toast.service.ts#ToastService.show`, `frontend/src/app/core/notifications/toast.service.ts#MAX_VISIBLE`, `frontend/src/app/core/notifications/toast.service.ts#AUTO_DISMISS_MS`, `frontend/src/app/core/notifications/toast-host.ts#ToastHost`).
- Light/dark/system theme with design tokens on `:root` and `[data-theme]` overrides (`frontend/src/app/core/theme/theme.service.ts#ThemeService`, `frontend/src/styles.scss`).
- Analysis scope (`repo` = "Whole repository", `user` = "My contributions") shared across pages (`frontend/src/app/core/scope/scope-preference.ts#ScopePreference`, `frontend/src/app/core/scope/scope-preference.ts#SCOPE_LABELS`, `frontend/src/app/core/models/api.models.ts#ScopeParam`).
- Static CSP as a meta tag, `connect-src` allowing `https:` because the API origin is runtime config (`frontend/src/index.html`).

## Owned entities
Client state only, all in-memory signals in root-provided services; no server data is written by this slice itself.
- Runtime config `{ apiBaseUrl }` and a load-failed flag (`frontend/src/app/core/config/app-config.ts#AppConfig`, `frontend/src/app/core/config/app-config.ts#AppConfigData`).
- Toast list (`frontend/src/app/core/notifications/toast.service.ts#ToastService.toasts`, `frontend/src/app/core/notifications/toast.service.ts#Toast`).
- Theme choice, persisted to storage key `devinsight.theme` (null for `system`), plus a derived effective theme (`frontend/src/app/core/theme/theme.service.ts#ThemeService.choice`, `frontend/src/app/core/theme/theme.service.ts#ThemeService.effective`, `frontend/src/app/core/theme/theme.service.ts#persistChoice`).
- Selected scope, default `repo`, not persisted (`frontend/src/app/core/scope/scope-preference.ts#ScopePreference.scope`).

## Happy paths
### App bootstrap to first authenticated request
1. Entrypoint: `bootstrapApplication(App, appConfig)` (`frontend/src/main.ts`).
2. An app initializer fetches `config.json` with `cache: 'no-store'` and stores the result, or sets the failure flag on any error or invalid value (`frontend/src/app/app.config.ts#appConfig`, `frontend/src/app/core/config/app-config.ts#loadAppConfig`, `frontend/src/app/core/config/app-config.ts#AppConfig.applyLoaded`).
3. The root component renders the config-error screen if loading failed; otherwise it renders header, router outlet and toast host and calls `SessionStore.ensureLoaded` (`frontend/src/app/app.ts#App`, `frontend/src/app/core/auth/session.store.ts#SessionStore.ensureLoaded`).
4. The session probe calls `GET /api/me` (`frontend/src/app/core/auth/session.store.ts#SessionStore.probe`, `frontend/src/app/core/api/profile.api.ts#ProfileApi.me`).
5. Interceptors run in order error, bearer, base URL: the stored token is added as `Authorization: Bearer` unless the request already has one, then the configured base URL is prefixed (`frontend/src/app/app.config.ts#appConfig`, `frontend/src/app/core/http/api.interceptors.ts#bearerTokenInterceptor`, `frontend/src/app/core/http/api.interceptors.ts#apiBaseUrlInterceptor`, `frontend/src/app/core/config/app-config.ts#AppConfig.apiUrl`, `frontend/src/app/core/auth/auth-token.store.ts#AuthTokenStore.token`).
6. Success sets the session to `authenticated` with the profile; a 401 on `/api/me` is treated as public by the error interceptor (no redirect), and the probe itself clears the token and marks the session anonymous (`frontend/src/app/core/auth/session.store.ts#SessionStore.setProfile`, `frontend/src/app/core/http/error.interceptor.ts#isPublicRequest`, `frontend/src/app/core/auth/session.store.ts#SessionStore.dropTokenIfRejected`).
7. The header then shows nav links and the avatar, or a Sign in button that sends the browser to the GitHub login URL (`frontend/src/app/core/layout/app-header.html`, `frontend/src/app/core/layout/app-header.ts#AppHeader.signIn`, `frontend/src/app/core/auth/session.store.ts#SessionStore.signIn`).

### API call through interceptors and error handling
1. Entrypoint: a feature calls a typed client method, e.g. `POST /api/repos/import` (`frontend/src/app/core/api/repos.api.ts#ReposApi.importFromGitHub`).
2. Bearer and base-URL interceptors rewrite the request as above (`frontend/src/app/core/http/api.interceptors.ts#bearerTokenInterceptor`, `frontend/src/app/core/http/api.interceptors.ts#apiBaseUrlInterceptor`).
3. On an `HttpErrorResponse` with 401 for a non-public path (anything but `/api/me` and `/api/portfolio/...`), the interceptor clears the token, marks the session signed out (which stops run polling) and navigates to `/` (`frontend/src/app/core/http/error.interceptor.ts#errorInterceptor`, `frontend/src/app/core/auth/auth-token.store.ts#AuthTokenStore.clear`, `frontend/src/app/core/auth/session.store.ts#SessionStore.markSignedOut`).
4. On any other status except 404, and unless `SILENT_ERRORS` is set (as the code exchange does), it builds a title/detail from the ProblemDetails body or a fallback and shows an error toast (`frontend/src/app/core/http/error.interceptor.ts#errorInterceptor`, `frontend/src/app/core/api/auth.api.ts#AuthApi.exchange`, `frontend/src/app/core/http/problem-details.ts#describeHttpError`, `frontend/src/app/core/notifications/toast.service.ts#ToastService.error`).
5. The error is always rethrown; callers using `UserActionErrors` get `undefined` for already-toasted errors, a "Not found" toast for 404, and a rethrow for anything else (`frontend/src/app/core/http/surfaced-errors.ts#UserActionErrors.handle`).
6. The toast appears in the host and is removed on click or after the auto-dismiss timeout (`frontend/src/app/core/notifications/toast-host.ts#ToastHost.dismiss`, `frontend/src/app/core/notifications/toast.service.ts#ToastService.dismiss`).

### Theme switching
1. Before first paint, an external script applies a stored `light`/`dark` choice to `<html data-theme>` (`frontend/src/index.html`, `frontend/public/theme-init.js`).
2. `ThemeService` starts from the stored choice (default `system`) and tracks `prefers-color-scheme: dark` changes (`frontend/src/app/core/theme/theme.service.ts#readStoredChoice`, `frontend/src/app/core/theme/theme.service.ts#ThemeService`).
3. Entrypoint: the header toggle button calls `toggle`, which sets the opposite of the effective theme (`frontend/src/app/core/layout/app-header.html`, `frontend/src/app/core/theme/theme.service.ts#ThemeService.toggle`).
4. An effect sets or removes `data-theme` on the document root and persists the choice through the storage helpers (`frontend/src/app/core/theme/theme.service.ts#ThemeService`, `frontend/src/app/core/theme/theme.service.ts#persistChoice`, `frontend/src/app/shared/util/browser-storage.ts#storeValueIfPossible`).
5. CSS tokens switch via `:root[data-theme='dark']` and the `prefers-color-scheme` block guarded by `:root:not([data-theme='light'])` (`frontend/src/styles.scss`).

## Unknowns
- Is anything subscribed to the errors reported by `provideBrowserGlobalErrorListeners` beyond Angular's default console logging (`frontend/src/app/app.config.ts#appConfig`)?
- A 412 gets a fallback toast saying GitHub access is missing and to sign in again (`frontend/src/app/core/http/problem-details.ts#FALLBACK_TITLES`); does any caller trigger re-sign-in for it, or is the toast the only response?
- `ScopePreference.scope` is an in-memory signal (`frontend/src/app/core/scope/scope-preference.ts#ScopePreference`); does any code outside this slice persist or restore it across reloads?
- What `apiBaseUrl` values are configured per environment beyond the Pages workflow's `API_URL` (`.github/workflows/deploy-pages.yml`)?
