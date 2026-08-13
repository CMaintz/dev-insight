# DevInsight

A developer insight and portfolio platform that imports GitHub repositories, analyzes developer activity and code quality, generates actionable feedback, and builds a public developer portfolio backed by real data.

> **Status:** In active development. The backend foundation (Spring Boot, hexagonal architecture, GitHub integration, persistence layer) is in place; see `project_overview.md` for the full specification and roadmap.

## What it does

- **GitHub login** — OAuth authentication, returning a JWT for API access
- **Repository import** — fetches a user's repositories via the GitHub API and stores them without duplicates; repositories can be toggled in/out of the portfolio
- **Analysis engine** — computes explainable metrics per repository, in two scopes (whole repo vs. the user's own contributions):
  - *Activity*: commit frequency, recency, totals
  - *Commit quality*: average commit size, size variance, vague-message detection ("fix", "stuff", "update")
  - *Structure*: file size distribution, files > 500 LOC, folder depth, monolith indicators
  - *Quality heuristics*: test presence, README presence, lint config, contributor count
  - *Languages*: distribution per repo and aggregated per user
- **Scoring** — metrics roll up into activity / structure / quality scores and an overall 0–100 score
- **Feedback** — rule-based feedback items with severity levels (AI feedback planned)
- **Dashboard & portfolio** — charts, time-series score evolution, language distribution, and a public portfolio page showing selected repositories (Angular frontend planned)

## Tech Stack

- **Java 21**, **Spring Boot 3.3** (Web, Security, OAuth2 Client, Data JPA, Validation)
- **Hexagonal architecture** (Ports & Adapters): `domain/{model,port,service}` core with `adapter/in/rest` and `adapter/out/{github,persistence}` adapters
- **PostgreSQL** + **Flyway** migrations
- **JWT** (jjwt) for stateless API auth
- **GitHub API** via `org.kohsuke:github-api`
- **Lombok**, **JUnit / Spring Security Test**
- **Maven**

## Architecture

```
src/main/java/com/devinsight/
├── domain/
│   ├── model/          Core entities (User, Repository, Analysis, Feedback…)
│   ├── port/in|out/    Use case and infrastructure interfaces
│   └── service/        Analysis, scoring, and feedback logic
├── adapter/
│   ├── in/rest/        REST controllers + DTOs
│   └── out/
│       ├── github/     GitHub API adapter
│       └── persistence/ JPA entities + repositories
└── infrastructure/config/  Security, OAuth, beans
```

All dependencies point inward toward the domain; the GitHub client, database, and REST layer are swappable adapters behind ports.

## Running

Requires Java 21, Maven, and a PostgreSQL instance (Flyway migrates the schema on startup).

```bash
mvn spring-boot:run
```

Tests:

```bash
mvn test
```
