# Deployment

```
Browser ──► GitHub Pages ─ https://cmaintz.github.io/DevInsight/   (Angular SPA, static)
   │
   └─────► Azure Container Apps ─ https://ca-devinsight-api.<env>.azurecontainerapps.io
              (ASP.NET Core API + background analysis worker, image from ghcr.io)
                      │
                      └─► PostgreSQL (Azure Database for PostgreSQL Flexible Server, or Supabase/Neon)
```

| Piece | Where | How it gets there |
|---|---|---|
| SPA | GitHub Pages | `.github/workflows/deploy-pages.yml` on every push to `main` touching `frontend/` |
| API image | `ghcr.io/cmaintz/devinsight-api` | `.github/workflows/deploy-api.yml` (build + push) |
| API rollout | Azure Container Apps | same workflow: `az containerapp update --image`, authenticated with **OIDC** (no Azure secret in GitHub) |
| Infrastructure | Azure resource group | `infra/main.bicep`, applied by `scripts/setup-azure.sh` (and re-applied whenever settings change) |

## First-time setup

```bash
bash scripts/setup-azure.sh
```

A 9-stage wizard: Azure sign-in → resource group → database choice + generated secrets → OIDC trust for
GitHub Actions → first image build → infrastructure → GitHub OAuth App → credentials → GitHub Pages.
Captured values are saved to `.env.azure` (git-ignored) so the wizard can be re-run safely.

## Cross-origin auth

The SPA and API are on different sites, so cookies would be third-party and blocked. Instead:

1. SPA → `GET {api}/api/auth/github/login?returnUrl=/dashboard` (full-page navigation).
2. GitHub → `{api}/api/auth/github/callback` → the API redirects to
   `https://cmaintz.github.io/DevInsight/auth/callback?code=<one-time code>&returnUrl=…`.
3. SPA → `POST {api}/api/auth/exchange {code}` → JWT, stored in `localStorage`, sent as `Authorization: Bearer`.

The code is random, single-use and valid for 60 seconds. CORS allows only the configured frontend origin.

## After the first sign-in

Profile data lives in the database, so it is entered in the app (**Settings**), not in code. From the About
page of cmaintz-site (`src/views/AboutView.astro`, English intro) and its `src/data/site.ts`:

- **LinkedIn:** `https://www.linkedin.com/in/christoffer-maintz/`
- **Bio** (about 1,000 characters; the limit is 2,000):

> I’m a developer in Aarhus who likes the unglamorous machinery that decides whether a team ships fast and safely: the pipeline, the gate, the tooling, the paved road. Lately that includes making AI coding agents genuinely useful without letting them quietly lower the bar.
>
> During my internship at WEXO I built a generative-AI platform for Shopware 6 as sole developer. It enriches a 3,000+ product catalogue in four languages, removed over a thousand hours of manual work, and is still business-critical today. I now work as a consultant on DevOps, automation and AI-assisted development, and build open-source developer tooling on the side: Foundry, a polyglot quality gate for AI-assisted development, and the jev-tools family.
>
> Before software I spent six years in retail leadership, which taught me more about processes, people and prioritising under pressure than any course. I’m looking for a role in DevOps, developer experience, platform engineering or AI enablement, and take on freelance work in the meantime.

Then tick **Publish portfolio**.

## Changing settings later

Edit `.env.azure` (or re-run the wizard), then:

```bash
set -a; source .env.azure; set +a
DEVINSIGHT_IMAGE=ghcr.io/cmaintz/devinsight-api:latest \
  az deployment group create -g "$AZURE_RESOURCE_GROUP" -f infra/main.bicep -p infra/main.bicepparam
```

The SPA reads the API URL at runtime from `config.json`, written by the Pages workflow from the
`DEVINSIGHT_API_URL` repository variable — change the variable and re-run the workflow; no rebuild needed.

## Costs and limits

- **Container Apps** (1 vCPU / 2 GiB, one analysis at a time) scales to zero; the monthly free grant covers portfolio traffic. The first request after
  an idle period is a cold start (~10–20 s). Max one replica (sign-in codes and the analysis queue are in memory).
- **Scheduled re-analysis** only runs while a replica is up; with scale-to-zero, history grows when the app is used.
- **PostgreSQL Flexible Server B1ms** is free for 12 months on a new Azure free account, then about USD 13/month.
  Supabase/Neon free tiers are the zero-cost alternative (Supabase pauses idle projects).
- **Log Analytics** (container logs, 30-day retention) is billed per GB ingested — cents per month at this volume.
- **AI feedback** (optional) is billed per analysis by Anthropic.

To tear everything down: `az group delete --name <resource group>`.
