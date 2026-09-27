# syntax=docker/dockerfile:1
# One image serves the API and the Angular SPA from the same origin, so the session
# cookie stays first-party and no CORS configuration is needed.

# --- Frontend -----------------------------------------------------------------------
FROM node:24.21.0-alpine AS web
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY frontend/ ./
RUN npm run build

# --- Backend ------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src/backend
COPY backend/global.json backend/Directory.Build.props backend/Directory.Packages.props backend/DevInsight.slnx ./
COPY backend/src/ src/
RUN dotnet publish src/DevInsight.Api/DevInsight.Api.csproj -c Release -o /app/publish -p:UseAppHost=false

# --- Runtime ------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# The analysis engine clones repositories with the git CLI.
RUN apt-get update \
    && apt-get install -y --no-install-recommends git ca-certificates \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=api /app/publish ./
COPY --from=web /src/frontend/dist/devinsight/browser ./wwwroot
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "DevInsight.Api.dll"]
