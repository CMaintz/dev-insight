using 'main.bicep'

// Non-secret settings. Secrets come from environment variables at deploy time (see docs/DEPLOYMENT.md),
// so nothing sensitive is committed.
param image = readEnvironmentVariable('DEVINSIGHT_IMAGE', 'ghcr.io/cmaintz/devinsight-api:latest')
param frontendUrl = 'https://cmaintz.github.io/DevInsight'
param deployPostgres = bool(readEnvironmentVariable('DEVINSIGHT_DEPLOY_POSTGRES', 'true'))
param externalConnectionString = readEnvironmentVariable('DEVINSIGHT_DB_CONNECTION', '')
param postgresAdminPassword = readEnvironmentVariable('DEVINSIGHT_DB_PASSWORD', '')
param githubClientId = readEnvironmentVariable('DEVINSIGHT_GITHUB_CLIENT_ID')
param githubClientSecret = readEnvironmentVariable('DEVINSIGHT_GITHUB_CLIENT_SECRET')
param jwtSigningKey = readEnvironmentVariable('DEVINSIGHT_JWT_SIGNING_KEY')
param anthropicApiKey = readEnvironmentVariable('DEVINSIGHT_ANTHROPIC_API_KEY', '')
