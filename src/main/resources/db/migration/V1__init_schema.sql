-- =============================================================================
-- V1__init_schema.sql
-- Initial schema for DevInsight.
--
-- Creates the core tables used by the hexagonal architecture persistence layer.
-- All primary keys use UUID type for global uniqueness and to avoid sequential
-- ID enumeration attacks.
--
-- Tables:
--   users            - Developer accounts authenticated via GitHub OAuth
--   repositories     - GitHub repositories imported by users
--   analyses         - Analysis runs against a repository
--   analysis_metrics - Raw named metrics collected during an analysis run
--   feedback         - Actionable feedback items generated from an analysis
-- =============================================================================

-- ---------------------------------------------------------------------------
-- users
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS users (
    id               UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    email            VARCHAR(255),
    github_id        VARCHAR(100) NOT NULL UNIQUE,
    github_username  VARCHAR(100) NOT NULL,
    linkedin_url     VARCHAR(500),
    bio              TEXT,
    created_at       TIMESTAMP   NOT NULL DEFAULT NOW()
);

COMMENT ON TABLE  users                IS 'Developer accounts authenticated via GitHub OAuth.';
COMMENT ON COLUMN users.github_id      IS 'Stable GitHub account/node ID — does not change even if the login changes.';
COMMENT ON COLUMN users.github_username IS 'GitHub login at the time of last login; updated on each auth.';

-- ---------------------------------------------------------------------------
-- repositories
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS repositories (
    id            UUID         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    user_id       UUID         NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    name          VARCHAR(255) NOT NULL,
    full_name     VARCHAR(500) NOT NULL,
    description   TEXT,
    language      VARCHAR(100),
    stars         INTEGER      NOT NULL DEFAULT 0,
    forks         INTEGER      NOT NULL DEFAULT 0,
    last_activity TIMESTAMP,
    selected      BOOLEAN      NOT NULL DEFAULT FALSE
);

COMMENT ON TABLE  repositories          IS 'GitHub repositories imported by a DevInsight user.';
COMMENT ON COLUMN repositories.selected IS 'TRUE if the user has chosen to feature this repo in their public portfolio.';

-- Index to speed up per-user repository lookups (used on dashboard and import)
CREATE INDEX IF NOT EXISTS idx_repositories_user_id
    ON repositories (user_id);

-- ---------------------------------------------------------------------------
-- analyses
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS analyses (
    id              UUID         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    repository_id   UUID         NOT NULL REFERENCES repositories(id) ON DELETE CASCADE,
    scope           VARCHAR(50)  NOT NULL,   -- AnalysisScope enum name: REPO | USER_CONTRIBUTION
    overall_score   INTEGER      NOT NULL DEFAULT 0,
    activity_score  INTEGER      NOT NULL DEFAULT 0,
    structure_score INTEGER      NOT NULL DEFAULT 0,
    quality_score   INTEGER      NOT NULL DEFAULT 0,
    created_at      TIMESTAMP    NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_analyses_overall_score   CHECK (overall_score   BETWEEN 0 AND 100),
    CONSTRAINT chk_analyses_activity_score  CHECK (activity_score  BETWEEN 0 AND 100),
    CONSTRAINT chk_analyses_structure_score CHECK (structure_score BETWEEN 0 AND 100),
    CONSTRAINT chk_analyses_quality_score   CHECK (quality_score   BETWEEN 0 AND 100)
);

COMMENT ON TABLE  analyses       IS 'Scored analysis runs for a repository. One canonical run per (repository_id, scope).';
COMMENT ON COLUMN analyses.scope IS 'AnalysisScope enum: REPO analyses the whole repo; USER_CONTRIBUTION filters to the user''s own commits/PRs.';

-- Composite index to support the findByRepositoryIdAndScope query and dashboard aggregation
CREATE INDEX IF NOT EXISTS idx_analyses_repository_id_scope
    ON analyses (repository_id, scope);

-- ---------------------------------------------------------------------------
-- analysis_metrics
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS analysis_metrics (
    id                UUID         NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    analysis_id       UUID         NOT NULL REFERENCES analyses(id) ON DELETE CASCADE,
    name              VARCHAR(255) NOT NULL,
    value             DOUBLE PRECISION NOT NULL,
    included_in_score BOOLEAN      NOT NULL DEFAULT FALSE
);

COMMENT ON TABLE  analysis_metrics                  IS 'Raw named metrics collected during an analysis run.';
COMMENT ON COLUMN analysis_metrics.name             IS 'Snake_case metric key, e.g. commit_count_last_30d.';
COMMENT ON COLUMN analysis_metrics.included_in_score IS 'TRUE if this metric feeds into a score dimension calculation.';

-- Index to speed up metric lookups by analysis
CREATE INDEX IF NOT EXISTS idx_analysis_metrics_analysis_id
    ON analysis_metrics (analysis_id);

-- ---------------------------------------------------------------------------
-- feedback
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS feedback (
    id          UUID        NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    analysis_id UUID        NOT NULL REFERENCES analyses(id) ON DELETE CASCADE,
    type        VARCHAR(50) NOT NULL,   -- FeedbackType enum name: AI | RULE_BASED
    message     TEXT        NOT NULL,
    severity    VARCHAR(20) NOT NULL,   -- Severity enum name: LOW | MEDIUM | HIGH

    CONSTRAINT chk_feedback_type     CHECK (type     IN ('AI', 'RULE_BASED')),
    CONSTRAINT chk_feedback_severity CHECK (severity IN ('LOW', 'MEDIUM', 'HIGH'))
);

COMMENT ON TABLE  feedback          IS 'Actionable feedback items generated from a completed analysis.';
COMMENT ON COLUMN feedback.type     IS 'FeedbackType: AI for LLM-generated feedback, RULE_BASED for threshold checks.';
COMMENT ON COLUMN feedback.severity IS 'Severity: LOW (informational), MEDIUM (should fix), HIGH (critical).';

-- Index to speed up feedback retrieval by analysis
CREATE INDEX IF NOT EXISTS idx_feedback_analysis_id
    ON feedback (analysis_id);
