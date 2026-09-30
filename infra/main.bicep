// DevInsight API on Azure Container Apps (+ optional Azure Database for PostgreSQL).
//
//   az deployment group create -g rg-devinsight -f infra/main.bicep -p infra/main.bicepparam
//
// The SPA is hosted on GitHub Pages; this template only runs the API. Secrets are passed as secure
// parameters and stored as Container Apps secrets, never as plain environment variables.
targetScope = 'resourceGroup'

@description('Short name used to derive resource names.')
param name string = 'devinsight'

param location string = resourceGroup().location

@description('Container image, e.g. ghcr.io/cmaintz/devinsight-api:latest. Updated on every deploy by CI.')
param image string

@description('Public URL of the SPA, e.g. https://cmaintz.github.io/dev-insight (CORS + post-login redirect).')
param frontendUrl string

@description('Create an Azure Database for PostgreSQL Flexible Server. False = bring your own (Supabase, Neon, …).')
param deployPostgres bool = true

@secure()
@description('Only when deployPostgres = false: a full Npgsql connection string.')
param externalConnectionString string = ''

@description('Only when deployPostgres = true.')
param postgresAdminLogin string = 'devinsight'

@secure()
@description('Only when deployPostgres = true.')
param postgresAdminPassword string = ''

@secure()
param githubClientId string

@secure()
param githubClientSecret string

@secure()
@description('At least 32 random bytes, e.g. openssl rand -base64 48.')
param jwtSigningKey string

@secure()
@description('Optional Anthropic API key; empty disables AI feedback.')
param anthropicApiKey string = ''

var suffix = uniqueString(resourceGroup().id)
var postgresServerName = 'psql-${name}-${suffix}'
var aiEnabled = !empty(anthropicApiKey)
var aiSecrets = aiEnabled ? [ { name: 'anthropic-api-key', value: anthropicApiKey } ] : []
var aiEnv = aiEnabled ? [ { name: 'AiFeedback__ApiKey', secretRef: 'anthropic-api-key' } ] : []
var connectionString = deployPostgres
  ? 'Host=${postgresServerName}.postgres.database.azure.com;Port=5432;Database=${name};Username=${postgresAdminLogin};Password=${postgresAdminPassword};SSL Mode=Require'
  : externalConnectionString

resource logs 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${name}-${suffix}'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource environment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-${name}'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
  }
}

resource postgres 'Microsoft.DBforPostgreSQL/flexibleServers@2025-08-01' = if (deployPostgres) {
  name: postgresServerName
  location: location
  sku: {
    name: 'Standard_B1ms'
    tier: 'Burstable'
  }
  properties: {
    version: '17'
    administratorLogin: postgresAdminLogin
    administratorLoginPassword: postgresAdminPassword
    storage: { storageSizeGB: 32 }
    backup: { backupRetentionDays: 7, geoRedundantBackup: 'Disabled' }
    highAvailability: { mode: 'Disabled' }
  }
}

resource database 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2025-08-01' = if (deployPostgres) {
  parent: postgres
  name: name
}

// 0.0.0.0 = "allow Azure services": Container Apps has no fixed outbound IP on the consumption plan.
resource allowAzure 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2025-08-01' = if (deployPostgres) {
  parent: postgres
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource api 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'ca-${name}-api'
  location: location
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'auto'
        allowInsecure: false
      }
      secrets: concat([
        { name: 'db-connection', value: connectionString }
        { name: 'github-client-id', value: githubClientId }
        { name: 'github-client-secret', value: githubClientSecret }
        { name: 'jwt-signing-key', value: jwtSigningKey }
      ], aiSecrets)
    }
    template: {
      containers: [
        {
          name: 'api'
          image: image
          resources: { cpu: json('1.0'), memory: '2Gi' }
          env: concat([
            { name: 'ConnectionStrings__DevInsight', secretRef: 'db-connection' }
            { name: 'GitHub__ClientId', secretRef: 'github-client-id' }
            { name: 'GitHub__ClientSecret', secretRef: 'github-client-secret' }
            { name: 'Jwt__SigningKey', secretRef: 'jwt-signing-key' }
            { name: 'Frontend__Url', value: frontendUrl }
            // One clone/analysis at a time keeps memory and ephemeral disk bounded on one small replica.
            { name: 'AnalysisWorker__MaxConcurrency', value: '1' }
            // The callback lives on the API's own ingress FQDN.
            { name: 'GitHub__CallbackUrl', value: 'https://ca-${name}-api.${environment.properties.defaultDomain}/api/auth/github/callback' }
          ], aiEnv)
          probes: [
            {
              type: 'Liveness'
              httpGet: { path: '/health', port: 8080 }
              initialDelaySeconds: 10
              periodSeconds: 30
            }
          ]
        }
      ]
      // One replica at most: sign-in codes and the analysis queue live in process memory.
      // Zero when idle keeps the app inside the free grant (first request after idle is a cold start).
      scale: { minReplicas: 0, maxReplicas: 1 }
    }
  }
  dependsOn: [ database, allowAzure ]
}

output apiUrl string = 'https://${api.properties.configuration.ingress.fqdn}'
output githubCallbackUrl string = 'https://${api.properties.configuration.ingress.fqdn}/api/auth/github/callback'
