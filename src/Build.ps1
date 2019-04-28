Write-Host "Generating Build Number"
$baseDate = [datetime]"01/01/2000"
$currentDate = $(Get-Date).ToUniversalTime()
$interval = NEW-TIMESPAN -Start $baseDate -End $currentDate
$days = $interval.Days
# Write-Host "##vso[task.setvariable variable=buildNumber]10.0.$days"

$ErrorActionPreference = 'Continue'
taskkill /T /F /IM dotnet.exe
taskkill /T /F /IM iisexpress.exe
taskkill /T /F /IM iisexpresstray.exe
taskkill /T /F /IM w3wp.exe
taskkill /T /F /IM msbuild.exe
taskkill /T /F /IM vbcscompiler.exe
iisreset /restart

dotnet clean -c Release
dotnet build -c Release -p:Version=10.0.$days