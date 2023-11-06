# ----------------------------------------------------------------------------------------
# Quick Start deployment script for running Moonglade on Microsoft Azure
# Author: Edi Wang
# ----------------------------------------------------------------------------------------
# You need to install Azure CLI and login to Azure before running this script.
# To install Azure CLI, please run:
# Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile .\AzureCLI.msi; Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'
# Reference: https://docs.microsoft.com/en-us/cli/azure/?view=azure-cli-latest

param(
    [bool] $useLinuxPlanWithDocker = 1
)

function Get-UrlStatusCode([string] $Url) {
    try {
        [System.Net.WebRequest]::Create($Url).GetResponse().StatusCode
    }
    catch [Net.WebException] {
        [int]$_.Exception.Response.StatusCode
    }
}

[Console]::ResetColor()
# az login --use-device-code
$output = az account show -o json | ConvertFrom-Json
$subscriptionList = az account list -o json | ConvertFrom-Json 
$subscriptionList | Format-Table name, id, tenantId -AutoSize

# Get subscription id

$subscriptionName = $output.name
Write-Host "Currently logged in to subscription """$output.name.Trim()""" in tenant " $output.tenantId
$subscriptionName = Read-Host "Enter subscription Id ("$output.id")"
$subscriptionName = $subscriptionName.Trim()
if ([string]::IsNullOrWhiteSpace($subscriptionName)) {
    $subscriptionName = $output.id
}
else {
    # az account set --subscription $subscriptionName
    Write-Host "Changed to subscription ("$subscriptionName")"
}

# Get region name

$regionName = Read-Host "Enter region name (default: East Asia)"
if ([string]::IsNullOrWhiteSpace($regionName)) {
    $regionName = "East Asia"
}
else {
    $regionName = $regionName.Trim()
}

while ($true) {
    $webAppName = Read-Host -Prompt "Enter webapp name"
    $webAppName = $webAppName.Trim()
    $keywords = @("xbox", "windows", "login", "microsoft")
    if ($keywords -contains $webAppName.ToLower()) {
        Write-Host "Webapp name cannot have keywords xbox, windows, login, microsoft"
        continue
    }
    # Create the request
    $HTTP_Status = Get-UrlStatusCode('https://' + $webAppName + '.azurewebsites.net')
    if ($HTTP_Status -eq 0) {
        break
    }
    else {
        Write-Host "Webapp name taken"
    }
}

# Start script
$rndNumber = Get-Random -Minimum 100 -Maximum 999
$rsgName = "moongladersg$rndNumber"
$dockerImageName = "ediwang/moonglade"
$aspName = "moongladeplan$rndNumber"
$storageAccountName = "moongladestorage$rndNumber"
$storageContainerName = "moongladeimages$rndNumber"
$sqlServerUsername = "moonglade"
$sqlServerName = "moongladesql$rndNumber"
$sqlDatabaseName = "moongladedb$rndNumber"

function Get-RandomCharacters($length, $characters) {
    $random = 1..$length | ForEach-Object { Get-Random -Maximum $characters.length }
    $private:ofs = ""
    return [String]$characters[$random]
}
 
function Scramble-String([string]$inputString) {     
    $characterArray = $inputString.ToCharArray()   
    $scrambledStringArray = $characterArray | Get-Random -Count $characterArray.Length     
    $outputString = -join $scrambledStringArray
    return $outputString 
}

$password = Get-RandomCharacters -length 5 -characters 'abcdefghiklmnoprstuvwxyz'
$password += Get-RandomCharacters -length 1 -characters 'ABCDEFGHKLMNOPRSTUVWXYZ'
$password += Get-RandomCharacters -length 1 -characters '1234567890'
$password += Get-RandomCharacters -length 1 -characters '!$%@#'
$password = Scramble-String $password

$sqlServerPassword = "m$password"

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
Clear-Host
Write-Host "Your Moonglade will be deployed to [$rsgName] in [$regionName] under Azure subscription [$subscriptionName]. Please confirm before continue." -ForegroundColor Green
if ($useLinuxPlanWithDocker) {
    Write-Host "+ Linux App Service Plan with Docker" -ForegroundColor Cyan
}

Read-Host -Prompt "Press [ENTER] to continue, [CTRL + C] to cancel"

# Select Subscription
$echo = az account set --subscription $subscriptionName
Write-Host "Selected Azure Subscription: " $subscriptionName -ForegroundColor Cyan

# Resource Group
$rsgExists = az group exists -n $rsgName
if ($rsgExists -eq 'false') {
    Write-Host "Creating Resource Group"
    $echo = az group create -l $regionName -n $rsgName
}

Write-Host "Deploying App Service Plan..." -ForegroundColor Green
# App Service Plan
$planCheck = az appservice plan list --query "[?name=='$aspName']" | ConvertFrom-Json
$planExists = $planCheck.Length -gt 0
if (!$planExists) {
    if ($useLinuxPlanWithDocker) {
        $echo = az appservice plan create -n $aspName -g $rsgName --is-linux --sku S1 --location $regionName
    }
    else {
        $echo = az appservice plan create -n $aspName -g $rsgName --sku S1 --location $regionName
    }
}

# Azure SQL
Write-Host "Deploying Azure SQL..." -ForegroundColor Green
$sqlServerCheck = az sql server list --query "[?name=='$sqlServerName']" | ConvertFrom-Json
$sqlServerExists = $sqlServerCheck.Length -gt 0
if (!$sqlServerExists) {
    Write-Host "Creating SQL Server"
    $echo = az sql server create --name $sqlServerName --resource-group $rsgName --location $regionName --admin-user $sqlServerUsername --admin-password $sqlServerPassword

    Write-Host "Setting Firewall to Allow Azure Connection"
    # When both starting IP and end IP are set to 0.0.0.0, the firewall is only opened for other Azure resources.
    $echo = az sql server firewall-rule create --resource-group $rsgName --server $sqlServerName --name AllowAllIps --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
}

$sqlDbCheck = az sql db list --resource-group $rsgName --server $sqlServerName --query "[?name=='$sqlDatabaseName']" | ConvertFrom-Json
$sqlDbExists = $sqlDbCheck.Length -gt 0
if (!$sqlDbExists) {
    Write-Host "Creating SQL Database"
    $echo = az sql db create --resource-group $rsgName --server $sqlServerName --name $sqlDatabaseName --service-objective S0 --backup-storage-redundancy Local
    Write-Host "SQL Server Password: $sqlServerPassword" -ForegroundColor Yellow
}

# Storage Account
Write-Host "Deploying Storage..." -ForegroundColor Green
$storageAccountCheck = az storage account list --query "[?name=='$storageAccountName']" | ConvertFrom-Json
$storageAccountExists = $storageAccountCheck.Length -gt 0
if (!$storageAccountExists) {
    Write-Host "Creating Storage Account"
    $echo = az storage account create --name $storageAccountName --resource-group $rsgName --location $regionName --sku Standard_LRS --kind StorageV2 --allow-blob-public-access true
}

$storageConn = az storage account show-connection-string -g $rsgName -n $storageAccountName | ConvertFrom-Json
$storageContainerExists = az storage container exists --name $storageContainerName --connection-string $storageConn.connectionString | ConvertFrom-Json
if (!$storageContainerExists.exists) {
    Write-Host "Creating storage container"
    $echo = az storage container create --name $storageContainerName --connection-string $storageConn.connectionString --public-access container
}

# Web App
$appCheck = az webapp list --query "[?name=='$webAppName']" | ConvertFrom-Json
$appExists = $appCheck.Length -gt 0
if (!$appExists) {
    Write-Host "Creating Web App"
    if ($useLinuxPlanWithDocker) {
        Write-Host "Using Linux Plan with Docker image from '$dockerImageName', this deployment will be ready to run."
        $echo = az webapp create -g $rsgName -p $aspName -n $webAppName --deployment-container-image-name $dockerImageName
    }
    else {
        Write-Host "Using Windows Plan with deployment from GitHub"
        $echo = az webapp create -g $rsgName -p $aspName -n $webAppName --runtime "DOTNET |8.0"
    }
    $echo = az webapp config set -g $rsgName -n $webAppName --always-on true --use-32bit-worker-process false --http20-enabled true
}

$createdApp = az webapp list --query "[?name=='$webAppName']" | ConvertFrom-Json
$createdExists = $createdApp.Length -gt 0
if ($createdExists) {
    $webAppUrl = "https://" + $createdApp.defaultHostName
    Write-Host "Web App URL: $webAppUrl"
}

# Configuration Update
Write-Host "Updating Configuration" -ForegroundColor Green

Write-Host "Setting SQL Database Connection String"
$sqlConnStrTemplate = az sql db show-connection-string -s $sqlServerName -n $sqlDatabaseName -c ado.net --auth-type SqlPassword
$sqlConnStr = $sqlConnStrTemplate.Replace("<username>", $sqlServerUsername).Replace("<password>", $sqlServerPassword)
$echo = az webapp config connection-string set -g $rsgName -n $webAppName -t SQLAzure --settings MoongladeDatabase=$sqlConnStr

Write-Host "Adding Blob Storage Connection String"
$scon = $storageConn.connectionString
if ($useLinuxPlanWithDocker) {
    $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage__Provider=azurestorage
    $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage__AzureStorageSettings__ConnectionString=$scon
    $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage__AzureStorageSettings__ContainerName=$storageContainerName
    $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ForwardedHeaders__UseForwardedHeaders=true
    $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
}
else {
    $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage:Provider=azurestorage
    $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage:AzureStorageSettings:ConnectionString=$scon
    $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage:AzureStorageSettings:ContainerName=$storageContainerName
}

if (!$useLinuxPlanWithDocker) {
    Write-Host "Pulling source code and run build on Azure (this takes time, please wait)..."
    $echo = az webapp deployment source config --branch release --manual-integration --name $webAppName --repo-url https://github.com/EdiWang/Moonglade --resource-group $rsgName
}

az webapp restart --name $webAppName --resource-group $rsgName

# Container warm up
if ($useLinuxPlanWithDocker) {
    Write-Host "Warming up the container..."
    Start-Sleep -Seconds 20
}

Read-Host -Prompt "Setup is done, you should be able to run Moonglade on '$webAppUrl' now, press [ENTER] to exit."