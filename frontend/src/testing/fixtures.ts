import {
  Analysis,
  AnalysisRun,
  Dashboard,
  Feedback,
  Portfolio,
  Profile,
  Project,
  Repository,
} from '../app/core/models/api.models';

export function aProfile(overrides: Partial<Profile> = {}): Profile {
  return {
    id: 'u1',
    login: 'octocat',
    name: 'The Octocat',
    email: null,
    avatarUrl: null,
    bio: null,
    linkedInUrl: null,
    isPortfolioPublic: false,
    portfolioPath: '/u/octocat',
    ...overrides,
  };
}

export function aRepo(overrides: Partial<Repository> = {}): Repository {
  return {
    id: 'r1',
    name: 'alpha',
    fullName: 'octocat/alpha',
    description: 'First repo',
    htmlUrl: 'https://github.com/octocat/alpha',
    language: 'TypeScript',
    stars: 3,
    forks: 1,
    isFork: false,
    isPrivate: false,
    isArchived: false,
    lastActivity: '2026-09-01T00:00:00+00:00',
    isSelected: true,
    languages: { TypeScript: 900, SCSS: 100 },
    ...overrides,
  };
}

export function aRun(overrides: Partial<AnalysisRun> = {}): AnalysisRun {
  return {
    id: 'run1',
    repositoryId: 'r1',
    scope: 'repo',
    status: 'queued',
    analysisId: null,
    error: null,
    requestedAt: '2026-09-27T12:00:00+00:00',
    startedAt: null,
    completedAt: null,
    ...overrides,
  };
}

export function aFeedback(overrides: Partial<Feedback> = {}): Feedback {
  return {
    id: 'f1',
    type: 'ruleBased',
    severity: 'medium',
    category: 'structure',
    source: 'monolith',
    title: 'Large files',
    message: 'Split big files.',
    isStrength: false,
    ...overrides,
  };
}

export function anAnalysis(overrides: Partial<Analysis> = {}): Analysis {
  return {
    id: 'a1',
    repositoryId: 'r1',
    scope: 'repo',
    createdAt: '2026-09-20T10:00:00+00:00',
    headCommitSha: 'abcdef1234567',
    overallScore: 72,
    scores: { activity: 80, structure: 60, quality: 75 },
    metrics: [
      {
        name: 'total_commits',
        category: 'activity',
        value: 120,
        includedInScore: true,
        points: 90,
        weight: 0.5,
      },
      {
        name: 'contributor_count',
        category: 'quality',
        value: 2,
        includedInScore: false,
        points: null,
        weight: null,
      },
    ],
    timeline: [{ weekStart: '2026-09-07', commits: 4, additions: 10, deletions: 2 }],
    largestFiles: [{ path: 'src/big.ts', lines: 812 }],
    feedback: [aFeedback(), aFeedback({ id: 'f2', isStrength: true, title: 'Has tests' })],
    ...overrides,
  };
}

export function aDashboard(overrides: Partial<Dashboard> = {}): Dashboard {
  return {
    scope: 'repo',
    repositoryCount: 2,
    selectedCount: 1,
    analyzedCount: 1,
    averageScores: { overall: 70, activity: 60, structure: 70, quality: 80 },
    repositories: [{ repository: aRepo(), scores: null, analyzedAt: null }],
    languages: [{ language: 'TypeScript', bytes: 900, share: 0.9 }],
    activity: [],
    scoreEvolution: [],
    commitQuality: { commits: 10, vagueCommits: 2, sizes: { xs: 1, s: 5, m: 3, l: 1, xl: 0 } },
    topFeedback: [{ repositoryId: 'r1', repositoryName: 'alpha', feedback: aFeedback() }],
    ...overrides,
  };
}

export function aProject(overrides: Partial<Project> = {}): Project {
  return {
    id: 'p1',
    name: 'Portfolio site',
    description: 'My site',
    imageUrls: ['https://example.com/a.png'],
    linkedRepositoryIds: ['r1'],
    sortOrder: 0,
    ...overrides,
  };
}

export function aPortfolio(overrides: Partial<Portfolio> = {}): Portfolio {
  return {
    owner: {
      login: 'octocat',
      name: 'The Octocat',
      avatarUrl: null,
      bio: 'I write code.',
      linkedInUrl: 'https://www.linkedin.com/in/octocat',
      gitHubUrl: 'https://github.com/octocat',
      isPublic: true,
    },
    scope: 'userContribution',
    scores: { overall: 81, activity: 70, structure: 85, quality: 88 },
    repositories: [
      {
        id: 'r1',
        name: 'alpha',
        description: 'First repo',
        htmlUrl: 'https://github.com/octocat/alpha',
        language: 'TypeScript',
        stars: 12,
        forks: 2,
        lastActivity: '2026-09-01T00:00:00+00:00',
        languages: { TypeScript: 800, HTML: 200 },
        scores: { overall: 81, activity: 70, structure: 85, quality: 88 },
      },
    ],
    projects: [
      {
        id: 'p1',
        name: 'Showcase',
        description: 'A project',
        imageUrls: ['https://example.com/1.png', 'https://example.com/2.png'],
        linkedRepositoryIds: ['r1'],
      },
    ],
    languages: [{ language: 'TypeScript', bytes: 800, share: 0.8 }],
    activity: [
      { weekStart: '2026-09-07', commits: 3, additions: 1, deletions: 1 },
      { weekStart: '2026-09-14', commits: 0, additions: 0, deletions: 0 },
    ],
    scoreEvolution: [],
    strengths: [{ repositoryId: 'r1', repositoryName: 'alpha', title: 'Tested', message: 'Good.' }],
    ...overrides,
  };
}
