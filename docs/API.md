# DevInsight HTTP API

Base path: `/api`. JSON uses camelCase; enums are camelCase strings. Timestamps are ISO-8601 UTC
(`2026-09-27T12:00:00+00:00`); dates (`weekStart`, `date`) are `YYYY-MM-DD`.

The live, generated contract is served by the API at `/openapi/v1.json` (browsable at `/scalar`
in Development). This document is the human-readable overview.

## Authentication

All protected endpoints take `Authorization: Bearer <jwt>`. The SPA may be hosted on another origin
(GitHub Pages), so there are no session cookies; sign-in hands the SPA a single-use code instead:

| Method | Path | Auth | Notes |
|---|---|---|---|
| GET | `/api/auth/github/login?returnUrl=/dashboard` | – | 302 to GitHub's consent screen. `returnUrl` must be a local path. |
| GET | `/api/auth/github/callback?code=…&state=…` | – | GitHub redirects here. Creates/updates the user, then 302 to `{Frontend:Url}/auth/callback?code=<one-time code>&returnUrl=…` (or `?error=signin_failed`). |
| POST | `/api/auth/exchange` | – | `{ code }` → `{ accessToken, expiresAt }`. Codes are single-use and expire after 60 s. |
| POST | `/api/auth/token` | ✓ | `{ accessToken, expiresAt }` — a fresh JWT for the signed-in user (scripts, API clients). |
| POST | `/api/auth/logout` | – | 204. Tokens are stateless; the client discards its token. |

CORS allows only the configured frontend origin (`Frontend:Url`).

Unauthenticated calls to protected endpoints return **401**. Resources owned by another user
return **404** (never 403), so IDs cannot be probed.

Errors are RFC 9457 problem details: `{ status, title, detail }`. Domain validation errors → 400;
missing GitHub token → 412.

## Types

```ts
type Scope = 'repo' | 'userContribution';          // query param also accepts 'user'
type Severity = 'low' | 'medium' | 'high';
type FeedbackType = 'ruleBased' | 'ai';
type MetricCategory = 'activity' | 'commitQuality' | 'structure' | 'quality';
type RunStatus = 'queued' | 'running' | 'succeeded' | 'failed';

interface Scores { overall: number; activity: number; structure: number; quality: number } // 0–100

interface Profile {
  id: string; login: string; name: string | null; email: string | null; avatarUrl: string | null;
  bio: string | null; linkedInUrl: string | null; isPortfolioPublic: boolean;
  portfolioPath: string;            // e.g. "/u/octocat" — the SPA route of the public portfolio
}

interface Repository {
  id: string; name: string; fullName: string; description: string | null; htmlUrl: string;
  language: string | null; stars: number; forks: number;
  isFork: boolean; isPrivate: boolean; isArchived: boolean;
  lastActivity: string | null; isSelected: boolean;
  languages: Record<string, number>;  // bytes per language
}

interface AnalysisRun {
  id: string; repositoryId: string; scope: Scope; status: RunStatus;
  analysisId: string | null; error: string | null;
  requestedAt: string; startedAt: string | null; completedAt: string | null;
}

interface Metric {
  name: string;                // stable key, e.g. "vague_commit_ratio" (see MetricKeys.cs)
  category: MetricCategory;
  value: number;
  includedInScore: boolean;
  points: number | null;       // 0–100 rating of this metric (scored metrics only)
  weight: number | null;       // share of its dimension score (scored metrics only)
}

interface ActivityWeek { weekStart: string; commits: number; additions: number; deletions: number }
interface FileSize { path: string; lines: number }

interface Feedback {
  id: string; type: FeedbackType; severity: Severity; category: MetricCategory;
  source: string;              // rule id, e.g. "monolith", or "ai"
  title: string; message: string; isStrength: boolean;
}

interface Analysis {
  id: string; repositoryId: string; scope: Scope; createdAt: string; headCommitSha: string | null;
  overallScore: number;
  scores: { activity: number; structure: number; quality: number };
  metrics: Metric[];
  timeline: ActivityWeek[];    // weekly, oldest first, up to 104 weeks
  largestFiles: FileSize[];    // top 10
  feedback: Feedback[];
}

interface ScoreSnapshot { analysisId: string; createdAt: string; overall: number; activity: number; structure: number; quality: number }
interface ScorePoint { date: string; overall: number; activity: number; structure: number; quality: number }
interface LanguageShare { language: string; bytes: number; share: number }   // share 0–1
interface CommitQuality { commits: number; vagueCommits: number; sizes: { xs: number; s: number; m: number; l: number; xl: number } }
// size buckets (lines changed): xs <10, s 10–49, m 50–249, l 250–999, xl ≥1000

interface RepositorySummary { repository: Repository; scores: Scores | null; analyzedAt: string | null }
interface FeedbackHighlight { repositoryId: string; repositoryName: string; feedback: Feedback }

interface Dashboard {
  scope: Scope;
  repositoryCount: number; selectedCount: number; analyzedCount: number;
  averageScores: Scores | null;            // over selected repositories
  repositories: RepositorySummary[];       // all repositories, selected first
  languages: LanguageShare[];              // selected repositories
  activity: ActivityWeek[];                // summed across selected repositories
  scoreEvolution: ScorePoint[];
  commitQuality: CommitQuality;
  topFeedback: FeedbackHighlight[];        // most severe improvement items
}

interface Project {
  id: string; name: string; description: string | null;
  imageUrls: string[]; linkedRepositoryIds: string[]; sortOrder: number;
}

interface Portfolio {
  owner: { login: string; name: string | null; avatarUrl: string | null; bio: string | null;
           linkedInUrl: string | null; gitHubUrl: string; isPublic: boolean };
  scope: 'userContribution';
  scores: Scores | null;
  repositories: Array<{ id: string; name: string; description: string | null; htmlUrl: string;
                        language: string | null; stars: number; forks: number; lastActivity: string | null;
                        languages: Record<string, number>; scores: Scores | null }>;
  projects: Array<{ id: string; name: string; description: string | null; imageUrls: string[]; linkedRepositoryIds: string[] }>;
  languages: LanguageShare[];
  activity: ActivityWeek[];
  scoreEvolution: ScorePoint[];
  strengths: Array<{ repositoryId: string; repositoryName: string; title: string; message: string }>;
}
```

## Endpoints

| Method | Path | Body → Response | Use case |
|---|---|---|---|
| GET | `/api/me` | → `Profile` | |
| PUT | `/api/me/profile` | `{ bio, linkedInUrl, isPortfolioPublic }` → `Profile` | UC6 |
| POST | `/api/repos/import` | → `{ imported, updated, total }` | UC2 |
| GET | `/api/repos` | → `Repository[]` | UC2 |
| GET | `/api/repos/{id}` | → `Repository` | UC2 |
| PATCH | `/api/repos/{id}/select` | `{ isSelected }` → `Repository` | UC2.1 |
| POST | `/api/analysis/run/{repoId}?scope=repo\|user` | → **202** `AnalysisRun` | UC3 |
| POST | `/api/analysis/run-all` | → **202** `AnalysisRun[]` (both scopes, all selected repos) | UC3 |
| GET | `/api/analysis/runs` | → `AnalysisRun[]` (recent 50) | |
| GET | `/api/analysis/runs/{runId}` | → `AnalysisRun` — poll until `succeeded`/`failed` | |
| GET | `/api/analysis/{repoId}?scope=repo\|user` | → `Analysis` (latest), 404 if never analysed | UC3 |
| GET | `/api/analysis/{repoId}/history?scope=` | → `ScoreSnapshot[]` oldest first | UC5 |
| GET | `/api/feedback/{analysisId}` | → `Feedback[]` | UC4 |
| GET | `/api/dashboard?scope=repo\|user` | → `Dashboard` | UC5 |
| GET | `/api/portfolio/{loginOrUserId}` | → `Portfolio` — **public**; 404 unless published (owner may preview) | UC6 |
| GET | `/api/projects` | → `Project[]` | |
| POST | `/api/projects` | `{ name, description, imageUrls, linkedRepositoryIds, sortOrder }` → **201** `Project` | |
| PUT | `/api/projects/{id}` | same body → `Project` | |
| DELETE | `/api/projects/{id}` | → 204 | |
| GET | `/health` | → 200 `Healthy` | |
