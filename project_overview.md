Developer Insight Platform – Spec
1. Product Overview
   A platform that:
   Imports GitHub repositories
   Analyzes developer activity and code quality
   Generates feedback
   Builds a dynamic public portfolio
   Shows data in dashboard + portfolio
   Visualizes development over time

2. Core Entities
   User
   id: UUID
   email: string
   githubId: string
   linkedinUrl: string (optional)
   bio: text (optional)
   createdAt: timestamp
   Repository
   id: UUID
   userId: UUID
   name: string
   language: string
   stars: int
   forks: int
   lastActivity: timestamp
   isSelected: boolean (default: true, controls inclusion in portfolio + aggregate metrics)
   Project (Future / Portfolio abstraction)
   id: UUID
   userId: UUID
   name: string
   description: text
   imageUrls: list
   linkedRepositoryIds: list
   Analysis (Entity)
   id: UUID
   repositoryId: UUID
   scope: string (REPO | USER_CONTRIBUTION)
   overallScore: int (0-100)
   activityScore: int
   structureScore: int
   qualityScore: int
   createdAt: timestamp
   Notes:
   Store separate Analysis per repository per scope
   Enables UI toggle between scopes
   AnalysisMetric
   id: UUID
   analysisId: UUID
   name: string
   value: number
   includedInScore: boolean
   Feedback (Entity)
   id: UUID
   analysisId: UUID
   type: string (AI | RULE_BASED)
   message: text
   severity: string (LOW, MEDIUM, HIGH)


3. Analysis System
   Metrics
   Activity
   Commit frequency (per week)
   Activity recency (days since last commit)
   Total commits
   Commit Quality
   Average commit size (lines changed)
   Commit size variance
   Commit message quality score
   Vague messages detection ("fix", "stuff", "update")
   Structure
   File size distribution
   Files > 500 LOC
   Folder depth
   Monolith indicator
   Quality (heuristic)
   Test presence
   README presence
   Lint/config presence
   Contributors count
   Languages
   Language distribution (%) per repo
   Aggregated language distribution per user
   Time-based Analysis
   Commit activity over time
   Score evolution (derived or snapshot-based)

4. Use Cases
   UC1: GitHub Login
   OAuth login
   Create/update user
   Return JWT
   UC2: Import Repositories
   Fetch repos
   Store without duplicates
   UC2.1: Select Repositories
   Toggle inclusion in portfolio
   UC3: Analyze Repository
   Compute metrics
   Support REPO + USER scopes
   UC4: Generate Feedback
   Rule-based feedback
   UC5: Dashboard
   Charts
   Time-series
   Language distribution
   Scope toggle
   UC6: Portfolio
   Public profile
   Shows selected repos
   Bio + LinkedIn link

5. API (Supplemental)
   Repositories
   PATCH /repos/{id}/select
   Analysis
   POST /analysis/run/{repoId}?scope=repo|user
   GET /analysis/{repoId}?scope=repo|user
   Response example: { overallScore, metrics: [], scores: { activity, structure, quality }, scope }
   Feedback
   GET /feedback/{analysisId}
   Portfolio
   GET /portfolio/{userId}
   Dashboard
   GET /dashboard

6. Architecture (Hexagonal - AKA Ports & Adapters)
   Core
   Entities
   Use cases
   Ports
   Example Port
   interface GitHubPort { List fetchRepositories(User user); }
   Ports
   GitHubPort
   AnalysisPort
   FeedbackPort
   AuthPort
   Adapters
   GitHub API adapter
   AI adapter (future)
   REST controllers
   Database (JPA)

7. Testing Strategy
   Unit
   Analysis logic
   Scoring
   Integration
   API
   DB
   Contract tests
   GitHub adapter (mocked)

8. Roadmap (Improved)
   Phase 1 – Foundation
   Project setup
   Database schema + migrations
   GitHub OAuth
   Phase 2 – Data Layer
   Repo import
   Persistence layer
   Basic endpoints
   Repository selection (toggle)
   Phase 3 – Basic Metrics
   Commit count
   Activity tracking
   Simple UI
   Phase 4 – Analysis Engine
   Advanced metrics
   Scoring system
   Phase 5 – Dashboard
   Charts
   Time-series visualizations
   Scope toggle (repo vs user)
   Language distribution views
   Phase 6 – Feedback System
   Rule-based feedback
   Phase 7 – Portfolio
   Public profile
   Phase 8 – Enhancements
   AI feedback
   Performance improvements

9. Future Extensions
   LinkedIn Integration
   Profile linking
   Optional data import (if API access)
   Recruiter View
   View candidates
   Filter/search
   Benchmarking
   Compare developers
   Team Analytics
   Analyze teams
   CI Integration
   Real test coverage
   Code Quality Integration
   Static analysis tools
   Gamification
   Achievements
   API Access
   Public API

10. AI Prompts (Feature-based)
    UC1 – GitHub Login
    "Implement GitHub OAuth login in Spring Boot.
    Create/update user
    Return JWT"
    UC2 – Import Repositories
    "Fetch repositories from GitHub API.
    Store in DB
    Avoid duplicates"
    UC3 – Analyze Repository
    "Analyze repository.
    Compute metrics (commits, size, files)
    Detect large files
    Score 0–100"
    UC4 – Generate Feedback
    "Generate feedback.
    Detect vague commit messages
    Detect missing tests
    Output multiple feedback items"
    UC5 – Dashboard
    "Build Angular dashboard.
    Show repos
    Show charts
    Show time-series data"
    UC6 – Portfolio
    "Build public portfolio.
    Public URL
    Show repos + scores
    Show bio"

11. Feature Backlog (Single Source of Truth)
    GitHub OAuth login
    Repository import (GitHub)
    Repository selection toggle (isSelected)
    Analysis (REPO scope)
    Analysis (USER_CONTRIBUTION scope)
    Commit metrics (count, frequency)
    Commit quality (size, message heuristics)
    Structure metrics (large files, depth)
    Quality heuristics (tests, README, lint)
    Language distribution (repo + user aggregate)
    Dashboard charts (activity, score, languages)
    Time-series (activity + score evolution)
    Public portfolio page
    LinkedIn link on profile

12. Notes
    Keep MVP simple
    Focus on explainable metrics
    Avoid over-engineering

12. Architecture (Condensed)
    Frontend (Angular)
    Dashboard
    Repo view
    Profile
    Backend
    GitHub integration
    Analysis engine
    Feedback system
    Pipeline
    Fetch repo data
    Analyze
    Store
    Visualize

