Write-Host "Generating Build Number"
$baseDate = [datetime]"01/01/2000"
$currentDate = $(Get-Date) # TODO: Use UTC Time
$interval = NEW-TIMESPAN -Start $baseDate -End $currentDate
$days = $interval.Days
# Write-Host "##vso[task.setvariable variable=buildNumber]10.0.$days.1024"

dotnet clean -c Release
dotnet build -c Release -p:Version=10.0.$days.1024