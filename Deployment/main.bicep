@description('The name of the web app')
param webAppName string

@description('The name of the App Service Plan')
param appServicePlanName string

@description('The name of the SQL Server')
@minLength(1)
@maxLength(63)
param sqlServerName string

@description('The name of the SQL Database')
param sqlDatabaseName string

@description('The name of the Storage Account')
@minLength(3)
@maxLength(24)
param storageAccountName string

@description('SQL Server admin username')
param sqlAdminUsername string

@description('SQL Server admin password')
@secure()
param sqlAdminPassword string

@description('Docker image name')
param dockerImageName string

@description('Location for all resources')
param location string = resourceGroup().location

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'P0v3'
    tier: 'Premium0V3'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminUsername
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
  }
}

// SQL Database
resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: {
    name: 'S0'
    tier: 'Standard'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    requestedBackupStorageRedundancy: 'Local'
  }
}

// SQL Firewall Rule - Allow Azure Services
resource sqlFirewallRule 'Microsoft.Sql/servers/firewallRules@2022-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAllAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Storage Account
resource storageAccount 'Microsoft.Storage/storageAccounts@2022-09-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
  }
}

resource fileService 'Microsoft.Storage/storageAccounts/fileServices@2022-09-01' = {
  parent: storageAccount
  name: 'default'
  properties: {}
}

resource primaryImageShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2022-09-01' = {
  parent: fileService
  name: 'moonglade-images'
  properties: {
    accessTier: 'TransactionOptimized'
    enabledProtocols: 'SMB'
  }
}

resource originalImageShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2022-09-01' = {
  parent: fileService
  name: 'moonglade-images-origin'
  properties: {
    accessTier: 'TransactionOptimized'
    enabledProtocols: 'SMB'
  }
}

// Web App
resource webApp 'Microsoft.Web/sites@2022-03-01' = {
  name: webAppName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOCKER|${dockerImageName}'
      alwaysOn: true
      use32BitWorkerProcess: false
      http20Enabled: true
      appSettings: [
        {
          name: 'WEBSITES_ENABLE_APP_SERVICE_STORAGE'
          value: 'false'
        }
        {
          name: 'DOCKER_REGISTRY_SERVER_URL'
          value: 'https://index.docker.io'
        }
        {
          name: 'ImageStorage__FileSystemPath'
          value: '/app/images'
        }
        {
          name: 'ImageStorage__OriginalFileSystemPath'
          value: '/app/images-origin'
        }
      ]
    }
    httpsOnly: true
  }
}

resource webAppStorageMounts 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: webApp
  name: 'azurestorageaccounts'
  properties: {
    primaryImages: {
      type: 'AzureFiles'
      accountName: storageAccount.name
      shareName: primaryImageShare.name
      accessKey: storageAccount.listKeys().keys[0].value
      mountPath: '/app/images'
    }
    originalImages: {
      type: 'AzureFiles'
      accountName: storageAccount.name
      shareName: originalImageShare.name
      accessKey: storageAccount.listKeys().keys[0].value
      mountPath: '/app/images-origin'
    }
  }
}

// Outputs
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
