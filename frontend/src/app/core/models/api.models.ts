/**
 * Types mirroring docs/API.md (the HTTP contract). Keep in sync with the backend.
 */

/** Scope as returned in response bodies. */
export type Scope = 'repo' | 'userContribution';
/** Scope as sent in the `?scope=` query parameter. */
export type ScopeParam = 'repo' | 'user';
export type Severity = 'low' | 'medium' | 'high';
export type FeedbackType = 'ruleBased' | 'ai';
export type MetricCategory = 'activity' | 'commitQuality' | 'structure' | 'quality';
export type RunStatus = 'queued' | 'running' | 'succeeded' | 'failed';

export interface Scores {
  overall: number;
  activity: number;
  structure: number;
  quality: number;
}

export interface Profile {
  id: string;
  login: string;
  name: string | null;
  email: string | null;
  avatarUrl: string | null;
  bio: string | null;
  linkedInUrl: string | null;
  isPortfolioPublic: boolean;
  portfolioPath: string;
}

export interface ProfileUpdate {
  bio: string | null;
  linkedInUrl: string | null;
  isPortfolioPublic: boolean;
}

export interface Repository {
  id: string;
  name: string;
  fullName: string;
  description: string | null;
  htmlUrl: string;
  language: string | null;
  stars: number;
  forks: number;
  isFork: boolean;
  isPrivate: boolean;
  isArchived: boolean;
  lastActivity: string | null;
  isSelected: boolean;
  languages: Record<string, number>;
}

export interface ImportResult {
  imported: number;
  updated: number;
  total: number;
}

export interface AnalysisRun {
  id: string;
  repositoryId: string;
  scope: Scope;
  status: RunStatus;
  analysisId: string | null;
  error: string | null;
  requestedAt: string;
  startedAt: string | null;
  completedAt: string | null;
}

export interface Metric {
  name: string;
  category: MetricCategory;
  value: number;
  includedInScore: boolean;
  points: number | null;
  weight: number | null;
}

export interface ActivityWeek {
  weekStart: string;
  commits: number;
  additions: number;
  deletions: number;
}

export interface FileSize {
  path: string;
  lines: number;
}

export interface Feedback {
  id: string;
  type: FeedbackType;
  severity: Severity;
  category: MetricCategory;
  source: string;
  title: string;
  message: string;
  isStrength: boolean;
}

export interface Analysis {
  id: string;
  repositoryId: string;
  scope: Scope;
  createdAt: string;
  headCommitSha: string | null;
  overallScore: number;
  scores: { activity: number; structure: number; quality: number };
  metrics: Metric[];
  timeline: ActivityWeek[];
  largestFiles: FileSize[];
  feedback: Feedback[];
}

export interface ScoreSnapshot {
  analysisId: string;
  createdAt: string;
  overall: number;
  activity: number;
  structure: number;
  quality: number;
}

export interface ScorePoint {
  date: string;
  overall: number;
  activity: number;
  structure: number;
  quality: number;
}

export interface LanguageShare {
  language: string;
  bytes: number;
  share: number;
}

export interface CommitSizes {
  xs: number;
  s: number;
  m: number;
  l: number;
  xl: number;
}

export interface CommitQuality {
  commits: number;
  vagueCommits: number;
  sizes: CommitSizes;
}

export interface RepositorySummary {
  repository: Repository;
  scores: Scores | null;
  analyzedAt: string | null;
}

export interface FeedbackHighlight {
  repositoryId: string;
  repositoryName: string;
  feedback: Feedback;
}

export interface Dashboard {
  scope: Scope;
  repositoryCount: number;
  selectedCount: number;
  analyzedCount: number;
  averageScores: Scores | null;
  repositories: RepositorySummary[];
  languages: LanguageShare[];
  activity: ActivityWeek[];
  scoreEvolution: ScorePoint[];
  commitQuality: CommitQuality;
  topFeedback: FeedbackHighlight[];
}

export interface Project {
  id: string;
  name: string;
  description: string | null;
  imageUrls: string[];
  linkedRepositoryIds: string[];
  sortOrder: number;
}

export type ProjectInput = Omit<Project, 'id'>;

export interface PortfolioOwner {
  login: string;
  name: string | null;
  avatarUrl: string | null;
  bio: string | null;
  linkedInUrl: string | null;
  gitHubUrl: string;
  isPublic: boolean;
}

export interface PortfolioRepository {
  id: string;
  name: string;
  description: string | null;
  htmlUrl: string;
  language: string | null;
  stars: number;
  forks: number;
  lastActivity: string | null;
  languages: Record<string, number>;
  scores: Scores | null;
}

export interface PortfolioProject {
  id: string;
  name: string;
  description: string | null;
  imageUrls: string[];
  linkedRepositoryIds: string[];
}

export interface PortfolioStrength {
  repositoryId: string;
  repositoryName: string;
  title: string;
  message: string;
}

export interface Portfolio {
  owner: PortfolioOwner;
  scope: 'userContribution';
  scores: Scores | null;
  repositories: PortfolioRepository[];
  projects: PortfolioProject[];
  languages: LanguageShare[];
  activity: ActivityWeek[];
  scoreEvolution: ScorePoint[];
  strengths: PortfolioStrength[];
}

/** RFC 9457 problem details body. */
export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
}

/** `POST /api/auth/exchange` and `POST /api/auth/token` response. */
export interface TokenResponse {
  accessToken: string;
  expiresAt: string;
}
