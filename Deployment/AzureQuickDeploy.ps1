# ---------------------------------------------------------------------------------------------------------
# Quick Start deployment script for running Moonglade on Microsoft Azure
# Author: Edi Wang
# ---------------------------------------------------------------------------------------------------------
# You need to install Azure CLI and login to Azure before running this script.
# To install Azure CLI, please run:
# Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile .\AzureCLI.msi; Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'
# Reference: https://docs.microsoft.com/en-us/cli/azure/?view=azure-cli-latest

param(
    $subscriptionName = "Microsoft MVP", 
    $regionName = "East Asia", 
    [bool] $useLinuxPlanWithDocker = 1, 
    [bool] $createCDN = 0
)

# Start script
$rndNumber = Get-Random -Minimum 100 -Maximum 999
$rsgName = "moongladersg$rndNumber"
$webAppName = "moongladeweb$rndNumber"
$aspName = "moongladeplan$rndNumber"
$storageAccountName = "moongladestorage$rndNumber"
$storageContainerName = "moongladeimages$rndNumber"
$sqlServerUsername = "moonglade"
$sqlServerName = "moongladesql$rndNumber"
$sqlDatabaseName = "moongladedb$rndNumber"
$cdnProfileName = "moongladecdn$rndNumber"
$cdnEndpointName = "moongladecep$rndNumber"

function Get-RandomCharacters($length, $characters) {
    $random = 1..$length | ForEach-Object { Get-Random -Maximum $characters.length }
    $private:ofs=""
    return [String]$characters[$random]
}
 
function Scramble-String([string]$inputString){     
    $characterArray = $inputString.ToCharArray()   
    $scrambledStringArray = $characterArray | Get-Random -Count $characterArray.Length     
    $outputString = -join $scrambledStringArray
    return $outputString 
}

$password = Get-RandomCharacters -length 5 -characters 'abcdefghiklmnoprstuvwxyz'
$password += Get-RandomCharacters -length 1 -characters 'ABCDEFGHKLMNOPRSTUVWXYZ'
$password += Get-RandomCharacters -length 1 -characters '1234567890'
$password += Get-RandomCharacters -length 1 -characters '!$%&@#'
$password = Scramble-String $password

$sqlServerPassword = $password
Write-Host "SQL Server Password: $sqlServerPassword" -ForegroundColor Yellow

function Check-Command($cmdname) {
    return [bool](Get-Command -Name $cmdname -ErrorAction SilentlyContinue)
}

if (Check-Command -cmdname 'az') {
    Write-Host "Azure CLI is found..."
}
else {
    Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile .\AzureCLI.msi; Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'
    Write-Host "Please run 'az-login' and re-execute this script"
    return
}

# Confirmation
Write-Host ""
Write-Host "Your Moonglade will be deployed to [$rsgName] in [$regionName] under Azure subscription [$subscriptionName]. Please confirm before continue." -ForegroundColor Green
if ($useLinuxPlanWithDocker) {
    Write-Host "+ Linux App Service Plan with Docker" -ForegroundColor Cyan
}
if ($createCDN) {
    Write-Host "+ CDN (10 minutes to propagate)" -ForegroundColor Cyan
}

Read-Host -Prompt "Press [ENTER] to continue, [CTRL + C] to cancel"

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
    if ($useLinuxPlanWithDocker) {
        az appservice plan create -n $aspName -g $rsgName --is-linux --sku S1 --location $regionName
    }
    else {
        az appservice plan create -n $aspName -g $rsgName --sku S1 --location $regionName
    }
}

# Web App
Write-Host ""
Write-Host "Preparing Web App" -ForegroundColor Green
$appCheck = az webapp list --query "[?name=='$webAppName']" | ConvertFrom-Json
$appExists = $appCheck.Length -gt 0
if (!$appExists) {
    Write-Host "Creating Web App"
    if ($useLinuxPlanWithDocker) {
        Write-Host "Using Linux Plan with Docker image from 'ediwang/moonglade', this deployment will be ready to run."
        az webapp create -g $rsgName -p $aspName -n $webAppName --deployment-container-image-name ediwang/moonglade
    }
    else {
        Write-Host "Using Windows Plan with deployment method as not set, you need to build and deploy the code yourself."
        az webapp create -g $rsgName -p $aspName -n $webAppName
    }
    az webapp config set -g $rsgName -n $webAppName --always-on true --use-32bit-worker-process false --http20-enabled true
}

$createdApp = az webapp list --query "[?name=='$webAppName']" | ConvertFrom-Json
$createdExists = $createdApp.Length -gt 0
if ($createdExists) {
    $webAppUrl = "https://" + $createdApp.defaultHostName
    Write-Host "Web App URL: $webAppUrl"
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

if ($createCDN) {
    # CDN
    Write-Host ""
    Write-Host "Preparing CDN" -ForegroundColor Green
    $cdnProfileCheck = az cdn profile list -g $rsgName --query "[?name=='$cdnProfileName']" | ConvertFrom-Json
    $cdnProfileExists = $cdnProfileCheck.Length -gt 0
    if (!$cdnProfileExists) {
        Write-Host "Creating CDN Profile"
        az cdn profile create --name $cdnProfileName --resource-group $rsgName --location $regionName --sku Standard_Microsoft

        Write-Host "Creating CDN Endpoint"
        $storageOrigion = "$storageAccountName.blob.core.windows.net"
        az cdn endpoint create -g $rsgName -n $cdnEndpointName --profile-name $cdnProfileName --origin $storageOrigion --origin-host-header $storageOrigion --enable-compression
    }
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
    az sql db create --resource-group $rsgName --server $sqlServerName --name $sqlDatabaseName --service-objective S0 --backup-storage-redundancy Local
}

# Configuration Update
Write-Host ""
Write-Host "Updating Configuration" -ForegroundColor Green

Write-Host "Setting SQL Database Connection String"
$sqlConnStrTemplate = az sql db show-connection-string -s $sqlServerName -n $sqlDatabaseName -c ado.net --auth-type SqlPassword
$sqlConnStr = $sqlConnStrTemplate.Replace("<username>", $sqlServerUsername).Replace("<password>", $sqlServerPassword)
az webapp config connection-string set -g $rsgName -n $webAppName -t SQLAzure --settings MoongladeDatabase=$sqlConnStr

Write-Host "Adding Blob Storage Connection String"
$scon = $storageConn.connectionString
if ($useLinuxPlanWithDocker) {
    az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage__Provider=azurestorage
    az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage__AzureStorageSettings__ConnectionString=$scon
    az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage__AzureStorageSettings__ContainerName=$storageContainerName
}
else {
    az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage:Provider=azurestorage
    az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage:AzureStorageSettings:ConnectionString=$scon
    az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage:AzureStorageSettings:ContainerName=$storageContainerName
}

if ($createCDN) {
    if ($useLinuxPlanWithDocker){
        az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage__CDNSettings__EnableCDNRedirect=true
        az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage__CDNSettings__CDNEndpoint="https://$cdnEndpointName.azureedge.net/$storageContainerName"
    }
    else{
        az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage:CDNSettings:EnableCDNRedirect=true
        az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage:CDNSettings:CDNEndpoint="https://$cdnEndpointName.azureedge.net/$storageContainerName"
    }
    
    Write-Host "It can take up to 10 minutes for endpoint '$cdnEndpointName.azureedge.net' settings to propagate." -ForegroundColor Yellow
}

if ($useLinuxPlanWithDocker) {
    Read-Host -Prompt "Setup is done, you should be able to run Moonglade on '$webAppUrl' now, press [ENTER] to exit."
}
else {
    Read-Host -Prompt "Setup is done, you can now deploy the code to '$webAppUrl', press [ENTER] to exit."
}