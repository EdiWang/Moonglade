param(
    [bool] $useLinuxPlanWithDocker = $true,
    [string] $defaultRegion = "West US"
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

function Create-ResourceGroup($rsgName, $regionName) {
    $rsgExists = az group exists -n $rsgName
    if ($rsgExists -eq 'false') {
        Write-Host "Creating Resource Group"
        $echo = az group create -l $regionName -n $rsgName
    }
}

function Create-AppServicePlan($aspName, $rsgName, $regionName, $useLinuxPlanWithDocker) {
    $planCheck = az appservice plan list --query "[?name=='$aspName']" | ConvertFrom-Json
    $planExists = $planCheck.Length -gt 0
    if (!$planExists) {
        if ($useLinuxPlanWithDocker) {
            $echo = az appservice plan create -n $aspName -g $rsgName --is-linux --sku P0V3 --location $regionName
        } else {
            $echo = az appservice plan create -n $aspName -g $rsgName --sku P0V3 --location $regionName
        }
    }
}

function Create-SqlServer($sqlServerName, $rsgName, $regionName, $sqlServerUsername, $sqlServerPassword) {
    $sqlServerCheck = az sql server list --query "[?name=='$sqlServerName']" | ConvertFrom-Json
    $sqlServerExists = $sqlServerCheck.Length -gt 0
    if (!$sqlServerExists) {
        Write-Host "Creating SQL Server"
        $echo = az sql server create --name $sqlServerName --resource-group $rsgName --location $regionName --admin-user $sqlServerUsername --admin-password $sqlServerPassword
        Write-Host "SQL Server Password: $sqlServerPassword" -ForegroundColor Yellow
        Write-Host "Setting Firewall to Allow Azure Connection"
        $echo = az sql server firewall-rule create --resource-group $rsgName --server $sqlServerName --name AllowAllIps --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0
    }
}

function Create-SqlDatabase($sqlDatabaseName, $rsgName, $sqlServerName) {
    $sqlDbCheck = az sql db list --resource-group $rsgName --server $sqlServerName --query "[?name=='$sqlDatabaseName']" | ConvertFrom-Json
    $sqlDbExists = $sqlDbCheck.Length -gt 0
    if (!$sqlDbExists) {
        Write-Host "Creating SQL Database"
        $echo = az sql db create --resource-group $rsgName --server $sqlServerName --name $sqlDatabaseName --service-objective S0 --backup-storage-redundancy Local
    }
}

function Create-StorageAccount($storageAccountName, $rsgName, $regionName) {
    $storageAccountCheck = az storage account list --query "[?name=='$storageAccountName']" | ConvertFrom-Json
    $storageAccountExists = $storageAccountCheck.Length -gt 0
    if (!$storageAccountExists) {
        Write-Host "Creating Storage Account"
        $echo = az storage account create --name $storageAccountName --resource-group $rsgName --location $regionName --sku Standard_LRS --kind StorageV2 --allow-blob-public-access true
    }
}

function Create-StorageContainer($storageContainerName, $storageConn) {
    $storageContainerExists = az storage container exists --name $storageContainerName --connection-string $storageConn.connectionString | ConvertFrom-Json
    if (!$storageContainerExists.exists) {
        Write-Host "Creating storage container"
        $echo = az storage container create --name $storageContainerName --connection-string $storageConn.connectionString --public-access container
    }
}

function Create-WebApp($webAppName, $rsgName, $aspName, $useLinuxPlanWithDocker, $dockerImageName) {
    $appCheck = az webapp list --query "[?name=='$webAppName']" | ConvertFrom-Json
    $appExists = $appCheck.Length -gt 0
    if (!$appExists) {
        Write-Host "Creating Web App"
        if ($useLinuxPlanWithDocker) {
            Write-Host "Using Linux Plan with Docker image from '$dockerImageName', this deployment will be ready to run."
            $echo = az webapp create -g $rsgName -p $aspName -n $webAppName --container-image-name $dockerImageName
        } else {
            Write-Host "Using Windows Plan with deployment from GitHub"
            $echo = az webapp create -g $rsgName -p $aspName -n $webAppName --runtime "DOTNET|8.0"
        }
        $echo = az webapp config set -g $rsgName -n $webAppName --always-on true --use-32bit-worker-process false --http20-enabled true
    }
}

function Update-WebAppConfig($webAppName, $rsgName, $sqlConnStr, $storageConn, $storageContainerName, $useLinuxPlanWithDocker) {
    Write-Host "Updating Configuration" -ForegroundColor Green
    Write-Host "Setting SQL Database Connection String"
    az webapp config connection-string set -g $rsgName -n $webAppName -t SQLAzure --settings MoongladeDatabase=$sqlConnStr

    Write-Host "Adding Blob Storage Connection String"
    $scon = $storageConn.connectionString
    if ($useLinuxPlanWithDocker) {
        $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage__Provider=azurestorage
        $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage__AzureStorageSettings__ConnectionString=$scon
        $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage__AzureStorageSettings__ContainerName=$storageContainerName
        $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ForwardedHeaders__UseForwardedHeaders=true
        $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
    } else {
        $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage:Provider=azurestorage
        $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage:AzureStorageSettings:ConnectionString=$scon
        $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage:AzureStorageSettings:ContainerName=$storageContainerName
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
} else {
    Write-Host "Changed to subscription ("$subscriptionName")"
}

# Select region
$regionName = Read-Host "Enter region name (default: $defaultRegion)"
if ([string]::IsNullOrWhiteSpace($regionName)) {
    $regionName = $defaultRegion
} else {
    $regionName = $regionName.Trim()
}

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
    } else {
        Write-Host "Webapp name taken"
    }
}

# Generate random names and passwords
$rndNumber = Get-Random -Minimum 1000 -Maximum 9999
$rsgName = "moongladersg$rndNumber"
$dockerImageName = "ediwang/moonglade"
$aspName = "moongladeplan$rndNumber"
$storageAccountName = "moongladestorage$rndNumber"
$storageContainerName = "moongladeimages$rndNumber"
$sqlServerUsername = "moonglade"
$sqlServerName = "moongladesql$rndNumber"
$sqlDatabaseName = "moongladedb$rndNumber"

$password = Get-RandomCharacters -length 5 -characters 'abcdefghiklmnoprstuvwxyz'
$password += Get-RandomCharacters -length 1 -characters 'ABCDEFGHKLMNOPRSTUVWXYZ'
$password += Get-RandomCharacters -length 1 -characters '1234567890'
$password += Get-RandomCharacters -length 1 -characters '!$%@#'
$password = Scramble-String $password
$sqlServerPassword = "m$password"

# Confirmation
Clear-Host
Write-Host "Your Moonglade will be deployed to [$rsgName] in [$regionName] under Azure subscription [$subscriptionName]. Please confirm before continue." -ForegroundColor Green
if ($useLinuxPlanWithDocker) {
    Write-Host "+ Linux App Service Plan with Docker" -ForegroundColor Cyan
}
Read-Host -Prompt "Press [ENTER] to continue, [CTRL + C] to cancel"

# Set subscription
az account set --subscription $subscriptionName
Write-Host "Selected Azure Subscription: " $subscriptionName -ForegroundColor Cyan

# Create resources
Create-ResourceGroup $rsgName $regionName
Create-AppServicePlan $aspName $rsgName $regionName $useLinuxPlanWithDocker
Create-SqlServer $sqlServerName $rsgName $regionName $sqlServerUsername $sqlServerPassword
Create-SqlDatabase $sqlDatabaseName $rsgName $sqlServerName
Create-StorageAccount $storageAccountName $rsgName $regionName

$storageConn = az storage account show-connection-string -g $rsgName -n $storageAccountName | ConvertFrom-Json
Create-StorageContainer $storageContainerName $storageConn

Create-WebApp $webAppName $rsgName $aspName $useLinuxPlanWithDocker $dockerImageName

$createdApp = az webapp list --query "[?name=='$webAppName']" | ConvertFrom-Json
$createdExists = $createdApp.Length -gt 0
if ($createdExists) {
    $webAppUrl = "https://" + $createdApp.defaultHostName
    Write-Host "Web App URL: $webAppUrl"
}

# Update configuration
$sqlConnStrTemplate = az sql db show-connection-string -s $sqlServerName -n $sqlDatabaseName -c ado.net --auth-type SqlPassword
$sqlConnStr = $sqlConnStrTemplate.Replace("<username>", $sqlServerUsername).Replace("<password>", $sqlServerPassword)
Update-WebAppConfig $webAppName $rsgName $sqlConnStr $storageConn $storageContainerName $useLinuxPlanWithDocker

if (!$useLinuxPlanWithDocker) {
    Write-Host "Pulling source code and run build on Azure (this takes time, please wait)..."
    az webapp deployment source config --branch release --manual-integration --name $webAppName --repo-url https://github.com/EdiWang/Moonglade --resource-group $rsgName
}

az webapp restart --name $webAppName --resource-group $rsgName

if ($useLinuxPlanWithDocker) {
    Write-Host "Warming up the container..."
    Start-Sleep -Seconds 20
}

Read-Host -Prompt "Setup is done, you should be able to run Moonglade on '$webAppUrl' now, press [ENTER] to exit."
