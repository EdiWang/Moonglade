# ---------------------------------------------------------------------------------------------------------
# Quick Start deployment script for running Moonglade on Microsoft Azure
# ---------------------------------------------------------------------------------------------------------
# Author: Edi Wang
# ---------------------------------------------------------------------------------------------------------
# You need to install Azure CLI and login to Azure before running this script.
# To install Azure CLI, please run:
# Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile .\AzureCLI.msi; Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'
# Reference: https://docs.microsoft.com/en-us/cli/azure/?view=azure-cli-latest

# Replace with your own values
$subscriptionName = "Microsoft MVP"
$rsgName = "Moonglade-Test-RSG"
$regionName = "East Asia"
$webAppName = "moonglade-test-web"
$aspName = "moonglade-test-plan"
$storageAccountName = "moongladeteststorage"
$storageContainerName = "moongladetestimages"
$sqlServerName = "moongladetestsqlsvr"
$sqlServerUsername = "moonglade"
$sqlServerPassword = "DotNetM00n8!@d3"
$sqlDatabaseName = "moonglade-test-db"
$cdnProfileName = "moonglade-test-cdn"
# TODO: CDN Endpoint, DNS Zone, Application Insight

# Select Subscription
az account set --subscription $subscriptionName
Write-Host "Selected Azure Subscription: " $subscriptionName -ForegroundColor Cyan

# Resource Group
Write-Host ""
Write-Host "Preparing Resource Group" -ForegroundColor Green
$rsgExists = az group exists -n $rsgName
if ($rsgExists -eq 'false') {
    Write-Host "Creating Resource Group"
    az group create -l $regionName -n $rsgName
}

# App Service Plan
Write-Host ""
Write-Host "Preparing App Service Plan" -ForegroundColor Green
$planCheck = az appservice plan list --query "[?name=='$aspName']" | ConvertFrom-Json
$planExists = $planCheck.Length -gt 0
if (!$planExists) {
    Write-Host "Creating App Service Plan"
    az appservice plan create -n $aspName -g $rsgName --sku S1 --location $regionName
}

# Web App
Write-Host ""
Write-Host "Preparing Web App" -ForegroundColor Green
$appCheck = az webapp list --query "[?name=='$webAppName']" | ConvertFrom-Json
$appExists = $appCheck.Length -gt 0
if (!$appExists) {
    Write-Host "Creating Web App"
    az webapp create -g $rsgName -p $aspName -n $webAppName
    az webapp config set -g $rsgName -n $webAppName --always-on true --use-32bit-worker-process false
}

# Storage Account
Write-Host ""
Write-Host "Preparing Storage Account" -ForegroundColor Green
$storageAccountCheck = az storage account list --query "[?name=='$storageAccountName']" | ConvertFrom-Json
$storageAccountExists = $storageAccountCheck.Length -gt 0
if (!$storageAccountExists) {
    Write-Host "Creating Storage Account"
    az storage account create --name $storageAccountName --resource-group $rsgName --location $regionName --sku Standard_LRS --kind StorageV2
}

$storageConn = az storage account show-connection-string -g $rsgName -n $storageAccountName | ConvertFrom-Json
$storageContainerExists = az storage container exists --name $storageContainerName --connection-string $storageConn.connectionString | ConvertFrom-Json
if (!$storageContainerExists.exists) {
    Write-Host "Creating storage container"
    az storage container create --name $storageContainerName --connection-string $storageConn.connectionString --public-access container
}

# Azure SQL
Write-Host ""
Write-Host "Preparing Azure SQL" -ForegroundColor Green
$sqlServerCheck = az sql server list --query "[?name=='$sqlServerName']" | ConvertFrom-Json
$sqlServerExists = $sqlServerCheck.Length -gt 0
if (!$sqlServerExists) {
    Write-Host "Creating SQL Server"
    az sql server create --name $sqlServerName --resource-group $rsgName --location $regionName --admin-user $sqlServerUsername --admin-password $sqlServerPassword
}

$sqlDbCheck = az sql db list --resource-group $rsgName --server $sqlServerName --query "[?name=='$sqlDatabaseName']" | ConvertFrom-Json
$sqlDbExists = $sqlDbCheck.Length -gt 0
if (!$sqlDbExists) {
    Write-Host "Creating SQL Database"
    az sql db create --resource-group $rsgName --server $sqlServerName --name $sqlDatabaseName --service-objective S0
}

# CDN
Write-Host ""
Write-Host "Preparing CDN" -ForegroundColor Green
$cdnProfileCheck = az cdn profile list -g $rsgName --query "[?name=='$cdnProfileName']" | ConvertFrom-Json
$cdnProfileExists = $cdnProfileCheck.Length -gt 0
if (!$cdnProfileExists) {
    Write-Host "Creating CDN Profile"
    az cdn profile create --name $cdnProfileName --resource-group $rsgName --location $regionName --sku Standard_Microsoft
}

Write-Host "Due to Edi doen't know how to associate CDN Endpoint to Blob Storage in Azure CLI, pleae go to Azure Portal and create an CDN Endpoint yourself..." -ForegroundColor Green

Read-Host -Prompt "Setup is done, you can now deploy the blog code, press [ENTER] to exit."