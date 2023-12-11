param(
    $regionName = "West Europe",
    [bool] $useLinuxPlanWithDocker = 1,
    [bool] $createCDN = 1
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

#while($true) {
#    $webAppName = Read-Host -Prompt "Enter webapp name"
#    $webAppName = $webAppName.Trim()
#    if($webAppName.ToLower() -match "xbox") {
#        Write-Host "Webapp name cannot have keywords xbox,windows,login,microsoft"
#        continue
#    } elseif ($webAppName.ToLower() -match "windows") {
#        Write-Host "Webapp name cannot have keywords xbox,windows,login,microsoft"
#        continue
#    } elseif ($webAppName.ToLower() -match "login") {
#        Write-Host "Webapp name cannot have keywords xbox,windows,login,microsoft"
#        continue
#    } elseif ($webAppName.ToLower() -match "microsoft") {
#        Write-Host "Webapp name cannot have keywords xbox,windows,login,microsoft"
#        continue
#    }
#    # Create the request
#    $HTTP_Status = Get-UrlStatusCode('https://' + $webAppName + '.azurewebsites.net')
#    if($HTTP_Status -eq 0) {
#        break
#    } else {
#        Write-Host "Webapp name taken"
#    }
#}

# Start script
$webAppName = Read-Host -Prompt "Enter webapp name"
$rndNumber = Read-Host -Prompt "Enter Ressource Group Number"
$rsgName = "moongladersg$rndNumber"
$storageAccountName = "moongladestorage$rndNumber"
$storageContainerName = "moongladeimages$rndNumber"
$cdnProfileName = "moongladecdn$rndNumber"
$cdnEndpointName = "moongladecep$rndNumber"

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
if ($createCDN) {
    Write-Host "+ CDN (10 minutes to propagate)" -ForegroundColor Cyan
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

if ($createCDN) {
    # CDN
    Write-Host "Deploying CDN..." -ForegroundColor Green
    $cdnProfileCheck = az cdn profile list -g $rsgName --query "[?name=='$cdnProfileName']" | ConvertFrom-Json
    $cdnProfileExists = $cdnProfileCheck.Length -gt 0
    if (!$cdnProfileExists) {
        Write-Host "Creating CDN Profile"
        $echo = az cdn profile create --name $cdnProfileName --resource-group $rsgName --location $regionName --sku Standard_Microsoft

        Write-Host "Creating CDN Endpoint"
        $storageOrigion = "$storageAccountName.blob.core.windows.net"
        $echo = az cdn endpoint create -g $rsgName -n $cdnEndpointName --profile-name $cdnProfileName --origin $storageOrigion --origin-host-header $storageOrigion --enable-compression
    }
}

if ($createCDN) {
    Write-Host "Configuring CDN endpoint for Image Storage"
    if ($useLinuxPlanWithDocker){
        $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage__CDNSettings__EnableCDNRedirect=true
        $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage__CDNSettings__CDNEndpoint="https://#$cdnEndpointName.azureedge.net/$storageContainerName"
    }
    else{
        $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage:CDNSettings:EnableCDNRedirect=true
        $echo = az webapp config appsettings set -g $rsgName -n $webAppName --settings ImageStorage:CDNSettings:CDNEndpoint="https://#$cdnEndpointName.azureedge.net/$storageContainerName"
    }

    Write-Host "It can take up to 10 minutes for endpoint '$cdnEndpointName.azureedge.net' to propagate, after that, please set CDN endpoint to 'https://#$cdnEndpointName.azureedge.net/$storageContainerName' in blog admin settings." -ForegroundColor Yellow
}

az webapp restart --name $webAppName --resource-group $rsgName

# Container warm up
Start-Sleep -Seconds 20

Read-Host -Prompt "Setup is done, you should be able to run Moonglade with CDN on '$cdnEndpointName' now, press [ENTER] to exit."
