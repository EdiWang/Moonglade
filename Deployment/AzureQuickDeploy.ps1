param(
    [string] $defaultRegion = "West US",
    [switch] $preRelease = $false
)

function Get-UrlStatusCode([string] $Url) {
    try {
        [System.Net.WebRequest]::Create($Url).GetResponse().StatusCode
    }
    catch [Net.WebException] {
        [int]$_.Exception.Response.StatusCode
    }
}

function Check-Command($cmdname) {
    return [bool](Get-Command -Name $cmdname -ErrorAction SilentlyContinue)
}

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

function Show-Menu {
    param (
        [string]$Title,
        [string[]]$Options,
        [int]$DefaultIndex = 0
    )
    
    $selectedIndex = $DefaultIndex
    $cursorVisible = [Console]::CursorVisible
    [Console]::CursorVisible = $false
    
    try {
        while ($true) {
            Clear-Host
            Write-Host $Title -ForegroundColor Green
            Write-Host ""
            
            for ($i = 0; $i -lt $Options.Length; $i++) {
                if ($i -eq $selectedIndex) {
                    Write-Host "  > $($Options[$i])" -ForegroundColor Cyan
                } else {
                    Write-Host "    $($Options[$i])"
                }
            }
            
            Write-Host ""
            Write-Host "Use arrow keys to select, press Enter to confirm" -ForegroundColor Gray
            
            $key = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
            
            switch ($key.VirtualKeyCode) {
                38 { # Up arrow
                    $selectedIndex = [Math]::Max(0, $selectedIndex - 1)
                }
                40 { # Down arrow
                    $selectedIndex = [Math]::Min($Options.Length - 1, $selectedIndex + 1)
                }
                13 { # Enter
                    return $Options[$selectedIndex]
                }
            }
        }
    }
    finally {
        [Console]::CursorVisible = $cursorVisible
    }
}

# Main script starts here
[Console]::ResetColor()

if (-not (Check-Command -cmdname 'az')) {
    Invoke-WebRequest -Uri https://aka.ms/installazurecliwindows -OutFile .\AzureCLI.msi
    Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'
    Write-Host "Please run 'az-login' and re-execute this script"
    return
}

# Login and select subscription
$output = az account show -o json | ConvertFrom-Json
$subscriptionList = az account list -o json | ConvertFrom-Json 
$subscriptionList | Format-Table name, id, tenantId -AutoSize

$subscriptionName = $output.name
Write-Host "Currently logged in to subscription """$output.name.Trim()""" in tenant " $output.tenantId
$subscriptionName = Read-Host "Enter subscription Id ("$output.id")"
$subscriptionName = $subscriptionName.Trim()
if ([string]::IsNullOrWhiteSpace($subscriptionName)) {
    $subscriptionName = $output.id
}
else {
    Write-Host "Changed to subscription ("$subscriptionName")"
}

# Select region
$azureRegions = @(
    "Australia Central",
    "Australia East",
    "Australia Southeast",
    "Brazil South",
    "Canada Central",
    "Canada East",
    "Central India",
    "Central US",
    "East Asia",
    "East US",
    "East US 2",
    "France Central",
    "Germany West Central",
    "Japan East",
    "Japan West",
    "Korea Central",
    "Korea South",
    "North Central US",
    "North Europe",
    "Norway East",
    "South Central US",
    "South India",
    "Southeast Asia",
    "Switzerland North",
    "UK South",
    "UK West",
    "West Central US",
    "West Europe",
    "West India",
    "West US",
    "West US 2",
    "West US 3"
)

$defaultIndex = [array]::IndexOf($azureRegions, $defaultRegion)
if ($defaultIndex -eq -1) { $defaultIndex = 0 }

$regionName = Show-Menu -Title "Select Azure Region:" -Options $azureRegions -DefaultIndex $defaultIndex
Write-Host "Selected region: $regionName" -ForegroundColor Cyan
Start-Sleep -Seconds 1

# Select web app name
while ($true) {
    $webAppName = Read-Host -Prompt "Enter webapp name"
    $webAppName = $webAppName.Trim()
    $keywords = @("xbox", "windows", "login", "microsoft")
    if ($keywords -contains $webAppName.ToLower()) {
        Write-Host "Webapp name cannot have keywords xbox, windows, login, microsoft"
        continue
    }
    $HTTP_Status = Get-UrlStatusCode('https://' + $webAppName + '.azurewebsites.net')
    if ($HTTP_Status -eq 0) {
        break
    }
    else {
        Write-Host "Webapp name taken"
    }
}

# Generate random names and passwords
$rndNumber = Get-Random -Minimum 1000 -Maximum 9999
$rsgName = "moongladersg$rndNumber"
$aspName = "moongladeplan$rndNumber"
$storageAccountName = "moongladestorage$rndNumber"
$sqlServerUsername = "moonglade"
$sqlServerName = "moongladesql$rndNumber"
$sqlDatabaseName = "moongladedb$rndNumber"

$password = Get-RandomCharacters -length 5 -characters 'abcdefghiklmnoprstuvwxyz'
$password += Get-RandomCharacters -length 1 -characters 'ABCDEFGHKLMNOPRSTUVWXYZ'
$password += Get-RandomCharacters -length 1 -characters '1234567890'
$password += Get-RandomCharacters -length 1 -characters '!$%@#'
$password = Scramble-String $password
$sqlServerPassword = "m$password"

# Set docker image name based on pre-release flag
if ($preRelease) {
    $dockerImageName = "ediwang/moonglade:preview"
}
else {
    $dockerImageName = "ediwang/moonglade"
}

# Confirmation
Clear-Host
Write-Host "Your Moonglade will be deployed to [$rsgName] in [$regionName] under Azure subscription [$subscriptionName]. Please confirm before continuing." -ForegroundColor Green
Write-Host "+ Linux App Service Plan with Docker" -ForegroundColor Cyan
Read-Host -Prompt "Press [ENTER] to continue, [CTRL + C] to cancel"

# Set subscription
az account set --subscription $subscriptionName
Write-Host "Selected Azure Subscription: " $subscriptionName -ForegroundColor Cyan

# Create Resource Group
Write-Host "Creating Resource Group: $rsgName" -ForegroundColor Green
$rsgExists = az group exists -n $rsgName
if ($rsgExists -eq 'false') {
    az group create -l $regionName -n $rsgName | Out-Null
}

# Get Bicep file path
$bicepFilePath = Join-Path $PSScriptRoot "main.bicep"

# Deploy using Bicep
Write-Host "Deploying Azure resources using Bicep..." -ForegroundColor Green
Write-Host "SQL Server Password: $sqlServerPassword" -ForegroundColor Yellow

$deploymentName = "moonglade-deployment-$(Get-Date -Format 'yyyyMMddHHmmss')"

$deploymentOutput = az deployment group create `
    --resource-group $rsgName `
    --name $deploymentName `
    --template-file $bicepFilePath `
    --parameters `
        webAppName=$webAppName `
        appServicePlanName=$aspName `
        sqlServerName=$sqlServerName `
        sqlDatabaseName=$sqlDatabaseName `
        storageAccountName=$storageAccountName `
        sqlAdminUsername=$sqlServerUsername `
        sqlAdminPassword=$sqlServerPassword `
        dockerImageName=$dockerImageName `
        location=$regionName `
    --output json | ConvertFrom-Json

if ($null -eq $deploymentOutput) {
    Write-Host "Deployment failed. Please check the error messages above." -ForegroundColor Red
    return
}

Write-Host "Deployment completed successfully!" -ForegroundColor Green

# Get outputs from Bicep deployment
$webAppUrl = $deploymentOutput.properties.outputs.webAppUrl.value
$sqlServerFqdn = $deploymentOutput.properties.outputs.sqlServerFqdn.value

# Build SQL connection string from known variables
$sqlConnStr = "Server=tcp:${sqlServerFqdn},1433;Initial Catalog=${sqlDatabaseName};Persist Security Info=False;User ID=${sqlServerUsername};Password=${sqlServerPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

# Retrieve storage connection string securely
$storageConnStr = az storage account show-connection-string -g $rsgName -n $storageAccountName --query connectionString -o tsv

Write-Host "Web App URL: $webAppUrl" -ForegroundColor Cyan

# Update Web App Configuration
Write-Host "Updating Web App Configuration..." -ForegroundColor Green

Write-Host "Setting SQL Database Connection String"
az webapp config connection-string set -g $rsgName -n $webAppName -t SQLAzure --settings MoongladeDatabase=$sqlConnStr | Out-Null

Write-Host "Adding Blob Storage Connection String and other settings"
az webapp config appsettings set -g $rsgName -n $webAppName --settings `
    ImageStorage__Provider=azurestorage `
    ImageStorage__AzureStorageSettings__ConnectionString=$storageConnStr `
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true | Out-Null

# Restart Web App
Write-Host "Restarting Web App..." -ForegroundColor Green
az webapp restart --name $webAppName --resource-group $rsgName | Out-Null

Write-Host "Warming up the container..."
Start-Sleep -Seconds 20

Read-Host -Prompt "Setup is done, you should be able to run Moonglade on '$webAppUrl' now, if you get a 502 error, please try restarting Moonglade from Azure App Service blade. Press [ENTER] to exit."
