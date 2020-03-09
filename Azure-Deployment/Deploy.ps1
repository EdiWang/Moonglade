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
# TODO: CDN Endpoint, DNS Zone, Application Insight, AAD

function Check-Command($cmdname) {
    return [bool](Get-Command -Name $cmdname -ErrorAction SilentlyContinue)
}

if(Check-Command -cmdname 'az') {
    Write-Host "Azure CLI is found on your machine. If something blow up, please check update for Azure CLI." -ForegroundColor Yellow
    az --version
}
else {
    Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile .\AzureCLI.msi; Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'
    az login
}

# Confirmation
Write-Host "Your Moonglade will be deployed to [$rsgName] in [$regionName] under Azure subscription [$subscriptionName]. Please confirm before continue."
Read-Host -Prompt "Press [ENTER] to continue"

# Select Subscription
az account set --subscription $subscriptionName
Write-Host "Selected Azure Subscription: " $subscriptionName -ForegroundColor Cyan

# Resource Group
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
    az webapp config set -g $rsgName -n $webAppName --always-on true --use-32bit-worker-process false --http20-enabled true
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

    Write-Host "Setting Firewall to Allow Azure Connection"
    # When both starting IP and end IP are set to 0.0.0.0, the firewall is only opened for other Azure resources.
    az sql server firewall-rule create --resource-group $rsgName --server $sqlServerName --name AllowAllIps --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
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

    # Write-Host "Creating CDN Endpoint"
    # $storageUrl = az storage blob url --connection-string $storageConn --container-name $storageContainerName --name "dummy"
    # $storageOrigion = $storageUrl.Replace("https://", "").Replace("/$storageContainerName/dummy", "");
    # az cdn endpoint create -g $rsgName -n endpoint --profile-name $cdnProfileName --origin-host-header $storageOrigion --enable-compression
}

# Configuration Update
Write-Host ""
Write-Host "Updating Configuration" -ForegroundColor Green

Write-Host "Setting SQL Database Connection String"
$sqlConnStrTemplate = az sql db show-connection-string -s $sqlServerName -n $sqlDatabaseName -c ado.net --auth-type SqlPassword
$sqlConnStr = $sqlConnStrTemplate.Replace("<username>", $sqlServerUsername).Replace("<password>", $sqlServerPassword)
az webapp config connection-string set -g $rsgName -n $webAppName -t SQLAzure --settings MoongladeDatabase=$sqlConnStr

Write-Host "Adding Blob Storage Connection String"
az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage:AzureStorageSettings:ConnectionString=$storageConn
az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage:AzureStorageSettings:ContainerName=$storageContainerName

Write-Host "Due to Edi doen't know how to associate CDN Endpoint to Blob Storage in Azure CLI, pleae go to Azure Portal and create a CDN Endpoint yourself..." -ForegroundColor Green

Read-Host -Prompt "Setup is done, you can now deploy the blog code, press [ENTER] to exit."