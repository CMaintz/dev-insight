---
sha: d4240bcfccce19669016317887401a47a79c4403
slice: insights
kind: domain
paths: [backend/src/DevInsight.Application/Dashboard, backend/src/DevInsight.Application/Portfolio, frontend/src/app/features/dashboard, frontend/src/app/features/portfolio]
---

## Purpose
Read-side aggregation of a user's repositories and their latest analyses into two views: the signed-in user's private dashboard (UC5, `backend/src/DevInsight.Application/Dashboard/GetDashboard.cs#GetDashboard`) and the public portfolio shown by handle (UC6, `backend/src/DevInsight.Application/Portfolio/GetPortfolio.cs#GetPortfolio`), with their Angular pages `frontend/src/app/features/dashboard/dashboard.page.ts#DashboardPage` and `frontend/src/app/features/portfolio/portfolio.page.ts#PortfolioPage`.

## Features
- View the dashboard for a scope, `repo` (whole repository, the default) or `user` (own contributions), toggled in the page (`frontend/src/app/shared/ui/scope-toggle.ts#ScopeToggle`, `frontend/src/app/core/scope/scope-preference.ts#ScopePreference`) and parsed server-side by `backend/src/DevInsight.Api/Endpoints/ApiEndpoints.cs#ScopeParser.Parse`.
- Dashboard aggregates cover selected repositories only: average scores, language shares, weekly activity, score evolution, commit-size and vague-commit totals (`backend/src/DevInsight.Application/Dashboard/GetDashboard.cs#GetDashboard.ExecuteAsync`, `backend/src/DevInsight.Application/Dashboard/Insights.cs#Insights`).
- Dashboard lists every repository, selected ones first then by last activity, each with its latest scores (`backend/src/DevInsight.Application/Dashboard/GetDashboard.cs#GetDashboard.ExecuteAsync`, `frontend/src/app/features/dashboard/repo-score-table.ts#RepoScoreTable`).
- Dashboard shows the top 8 non-strength feedback items by severity (`backend/src/DevInsight.Application/Dashboard/GetDashboard.cs#GetDashboard.TopFeedback`).
- A first-run guide (import, select, analyse, view, publish) shows while nothing is analysed or the portfolio is not public (`frontend/src/app/features/dashboard/dashboard.page.ts#DashboardPage.showGuide`, `frontend/src/app/features/dashboard/first-run-guide.ts#GUIDE_STEPS`).
- The dashboard reloads when an analysis run settles or an import completes (`frontend/src/app/features/dashboard/dashboard.page.ts#DashboardPage`, `frontend/src/app/shared/util/reload-on.ts#reloadOn`).
- Anyone can view a portfolio at `/u/:handle` by GitHub login or user id; an unpublished one is visible only to its owner, who sees an owner-preview state (`backend/src/DevInsight.Application/Portfolio/GetPortfolio.cs#GetPortfolio.FindVisibleOwnerAsync`, `frontend/src/app/features/portfolio/portfolio.page.ts#PortfolioPage.isOwnerPreview`).
- The portfolio shows selected, non-private repositories in USER_CONTRIBUTION scope, strengths only, and the owner's projects with links to hidden repositories removed (`backend/src/DevInsight.Application/Portfolio/GetPortfolio.cs#GetPortfolio.ExecuteAsync`, `backend/src/DevInsight.Application/Portfolio/GetPortfolio.cs#GetPortfolio.ToPublic`, `backend/src/DevInsight.Application/Portfolio/GetPortfolio.cs#GetPortfolio.PortfolioScope`).
- Portfolio KPI tiles (repository count, commits, active weeks, top language) are computed client-side (`frontend/src/app/features/portfolio/portfolio.page.ts#portfolioHighlights`).

## Owned entities
None. The slice only reads users, repositories, analyses and projects through the store interfaces (`backend/src/DevInsight.Application/Abstractions/Stores.cs#IRepositoryStore`, `backend/src/DevInsight.Application/Abstractions/Stores.cs#IAnalysisStore`, `backend/src/DevInsight.Application/Abstractions/Stores.cs#IUserStore`, `backend/src/DevInsight.Application/Abstractions/Stores.cs#IProjectStore`). The view records it returns are defined in `backend/src/DevInsight.Application/Dashboard/GetDashboard.cs#DashboardView` and `backend/src/DevInsight.Application/Portfolio/GetPortfolio.cs#PortfolioView`.

## Happy paths
### Load the dashboard
1. Route `/dashboard` behind `authGuard` lazy-loads the page (`frontend/src/app/app.routes.ts#routes`, `frontend/src/app/features/dashboard/dashboard.page.ts#DashboardPage`).
2. The page builds an `httpResource` on `GET /api/dashboard?scope=<scope>` from the shared scope signal (`frontend/src/app/core/api/dashboard.api.ts#DashboardApi.resource`) and keeps the last value across reloads (`frontend/src/app/shared/util/reload-on.ts#stickyValue`).
3. The endpoint requires authorization, binds the user id from the token and parses the scope (`backend/src/DevInsight.Api/Endpoints/ApiEndpoints.cs#ApiEndpoints.MapInsights`, `backend/src/DevInsight.Api/Auth/CurrentUser.cs#CurrentUser.BindAsync`, `backend/src/DevInsight.Api/Endpoints/ApiEndpoints.cs#ScopeParser.Parse`).
4. `GetDashboard` (scoped, `backend/src/DevInsight.Application/DependencyInjection.cs#DependencyInjection.AddApplication`) lists the user's repositories (`backend/src/DevInsight.Infrastructure/Persistence/Stores/EfStores.cs#EfRepositoryStore.ListForUserAsync`), then loads the latest analysis per repository and the score history in that scope (`backend/src/DevInsight.Application/Dashboard/RepositoryInsights.cs#RepositoryInsights.LoadAsync`, `backend/src/DevInsight.Infrastructure/Persistence/Stores/EfAnalysisStore.cs#EfAnalysisStore.GetLatestForRepositoriesAsync`, `backend/src/DevInsight.Infrastructure/Persistence/Stores/EfAnalysisStore.cs#EfAnalysisStore.GetScoreHistoryAsync`).
5. It filters to selected repositories (`backend/src/DevInsight.Application/Dashboard/RepositoryInsights.cs#RepositoryInsights.Where`) and aggregates them (`backend/src/DevInsight.Application/Dashboard/Insights.cs#Insights.AverageScores`, `backend/src/DevInsight.Application/Dashboard/Insights.cs#Insights.ScoreEvolution`, `backend/src/DevInsight.Application/Dashboard/Insights.cs#Insights.CommitQuality`).
6. The view is mapped to `DashboardDto` and returned (`backend/src/DevInsight.Api/Contracts/Mapping.cs#Mapping.ToDto`, `backend/src/DevInsight.Api/Contracts/Dtos.cs#DashboardDto`). No writes or side effects.

### View a public portfolio by handle
1. Route `/u/:handle` (no guard) lazy-loads the page; the `handle` route param binds to the component input (`frontend/src/app/app.routes.ts#routes`, `frontend/src/app/app.config.ts#appConfig`, `frontend/src/app/features/portfolio/portfolio.page.ts#PortfolioPage.handle`).
2. The page requests `GET /api/portfolio/<handle>` (`frontend/src/app/core/api/portfolio.api.ts#PortfolioApi.resource`).
3. The endpoint allows anonymous callers and passes the optional signed-in user id as viewer (`backend/src/DevInsight.Api/Endpoints/ApiEndpoints.cs#ApiEndpoints.MapInsights`, `backend/src/DevInsight.Api/Auth/SessionTokens.cs#ClaimsPrincipalExtensions.FindUserId`).
4. The owner is resolved by id when the handle parses as a GUID, else by case-insensitive login; if missing, or unpublished and not the viewer, `NotFoundException` is thrown (`backend/src/DevInsight.Application/Portfolio/GetPortfolio.cs#GetPortfolio.FindVisibleOwnerAsync`, `backend/src/DevInsight.Infrastructure/Persistence/Stores/EfStores.cs#EfUserStore.FindByLoginAsync`), which becomes a 404 (`backend/src/DevInsight.Api/Infrastructure/ProblemDetailsExceptionHandler.cs#ProblemDetailsExceptionHandler.TryHandleAsync`) and the page's not-found state (`frontend/src/app/features/portfolio/portfolio.page.ts#PortfolioPage.notFound`).
5. Selected, non-private repositories are loaded with their USER_CONTRIBUTION analyses, ordered by overall score then stars; projects are listed by sort order (`backend/src/DevInsight.Application/Portfolio/GetPortfolio.cs#GetPortfolio.ExecuteAsync`, `backend/src/DevInsight.Infrastructure/Persistence/Stores/EfStores.cs#EfProjectStore.ListForUserAsync`).
6. Only strength feedback is included (`backend/src/DevInsight.Application/Dashboard/RepositoryInsights.cs#RepositoryInsights.Highlights`); the view maps to `PortfolioDto` with a GitHub profile URL (`backend/src/DevInsight.Api/Contracts/Mapping.cs#Mapping.ToDto`, `backend/src/DevInsight.Api/Contracts/Dtos.cs#PortfolioDto`).
7. The page sets the browser title to the owner's name and renders hero, KPIs, charts, strengths, projects and repository cards (`frontend/src/app/features/portfolio/portfolio.page.ts#PortfolioPage`, `frontend/src/app/features/portfolio/portfolio-hero.ts#PortfolioHero`, `frontend/src/app/features/portfolio/portfolio-cards.ts#PortfolioRepoCard`, `frontend/src/app/features/portfolio/portfolio-cards.ts#PortfolioProjectCard`).

## Unknowns
- `EfUserStore.FindByLoginAsync` picks the most recently updated user when several share a login: under what circumstances can two users hold the same login?
- Where is `Feedback.Severity` assigned, and is its ordering what the dashboard's "top feedback" intends?
- The portfolio KPI tile labels commits "last 2 years": is that window enforced by the analysis that produces `Timeline`, outside this slice?
- When a project links only repositories that are hidden, is the project itself still meant to appear on the public portfolio (the code keeps it with an empty link list)?
