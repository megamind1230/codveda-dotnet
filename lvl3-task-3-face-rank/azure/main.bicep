param location string = resourceGroup().location
param appName string = 'facerank-${uniqueString(resourceGroup().id)}'
param storageSku string = 'Standard_LRS'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: '${replace(appName, '-', '')}stg'
  location: location
  kind: 'StorageV2'
  sku: { name: storageSku }
  properties: {
    minimumTlsVersion: 'TLS1_2'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  name: 'default'
  parent: storage
}

resource avatarContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  name: 'avatars'
  parent: blobService
  properties: { publicAccess: 'Blob' }
}

resource appPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${appName}-plan'
  location: location
  sku: { name: 'F1', tier: 'Free' }
}

resource webApp 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  properties: {
    serverFarmId: appPlan.id
    siteConfig: {
      appSettings: [
        { name: 'ConnectionStrings:DefaultDb', value: 'Data Source=/home/site/wwwroot/FaceRank.db' }
        { name: 'Azure:BlobStorage:ConnectionString', value: storage.listKeys().keys[0].value }
        { name: 'Azure:BlobStorage:ContainerName', value: 'avatars' }
        { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
      ]
    }
  }
}

resource funcApp 'Microsoft.Web/sites@2023-12-01' = {
  name: '${appName}-func'
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: appPlan.id
    siteConfig: {
      appSettings: [
        { name: 'AzureWebJobsStorage', value: 'DefaultEndpointsProtocol=https;AccountName=${storage.name};AccountKey=${storage.listKeys().keys[0].value}' }
        { name: 'FUNCTIONS_WORKER_RUNTIME', value: 'dotnet' }
        { name: 'ConnectionStrings:DefaultDb', value: 'Data Source=/home/site/wwwroot/FaceRank.db' }
      ]
    }
  }
}
