# .NET Code Metrics Script
# Author: Edi Wang
# Reference: https://docs.microsoft.com/en-us/visualstudio/code-quality/how-to-generate-code-metrics-data?view=vs-2019

param(
    $targetPath = "C:\GitHub\Moonglade\src",
    $metricsPath = "C:\CodeMetricsResult",
    [bool] $useMetricsExe = 1,
    $metricsExePath = "C:\Tools\Metrics\Release\net472"
)

Clear-Host
Write-Host "This script will run Code Metrics on [$targetPath], output to [$metricsPath]. Please confirm before continue." -ForegroundColor Green
if ($useMetricsExe) {
    if (Test-Path $metricsExePath) {
        Write-Host "'$metricsExePath' exists, remember to check update regularly." -ForegroundColor Gray           
    }
    else {
        Write-Host "'$metricsExePath' does not exist, downloading pre-compiled package..." -ForegroundColor Yellow
        Invoke-WebRequest -Uri "https://go.edi.wang/aka/metrics" -OutFile "Metrics.zip"
        Expand-Archive -Path Metrics.zip -DestinationPath $metricsExePath.Replace("Metrics\Release\net472", "") -Force
    }

    Write-Host "+ Using pre-compiled Metrics.exe at '$metricsExePath'" -ForegroundColor Cyan
}
else {
    Write-Host "+ Using 'Microsoft.CodeAnalysis.Metrics', this will only support .NET Core or .NET 5 projects." -ForegroundColor Cyan
}

Read-Host -Prompt "Press [ENTER] to continue, [CTRL + C] to cancel"

$vsPath = &(Join-Path ${env:ProgramFiles(x86)} "\Microsoft Visual Studio\Installer\vswhere.exe") -property installationpath
Import-Module (Get-ChildItem $vsPath -Recurse -File -Filter Microsoft.VisualStudio.DevShell.dll).FullName
Enter-VsDevShell -VsInstallPath $vsPath -SkipAutomaticLocation

Write-Host "Creating output directory '$metricsPath'" -ForegroundColor Yellow
$echo = New-Item -ItemType Directory -Force $metricsPath

cd $targetPath
Write-Host "Finding C# project files in '$targetPath'" -ForegroundColor Yellow
Get-ChildItem -Path $targetPath -Filter *.csproj -Recurse -File | ForEach-Object {
    $csprojPath = $_.FullName
    $projName = $_.Name
    $dirName = $_.DirectoryName

    if ($csprojPath.EndsWith("Tests.csproj")) {
        Write-Host "Skipped Unit Test project '$projName'" -ForegroundColor Gray
    }
    else {
        Write-Host "Running Code Metrics' for '$projName'" -ForegroundColor Cyan
        if ($useMetricsExe) {
            cd $metricsExePath
            $echo = .\Metrics.exe /project:$csprojPath /out:$metricsPath/$projName.xml
        }
        else {
            Write-Host "Adding 'Microsoft.CodeAnalysis.Metrics' to '$projName'" 

            $echo = dotnet add $csprojPath package Microsoft.CodeAnalysis.Metrics
            $echo = MSBuild $csprojPath /t:Metrics /p:MetricsOutputFile=$metricsPath/$projName.xml
        }
    }
}

# Show report
Get-ChildItem -Path $metricsPath -Filter *.csproj.xml -Recurse -File | ForEach-Object {
    $xmlPath = $_.FullName
    $xmlName = $_.Name
    
    Write-Host "Code Metrics for '$xmlName':" -ForegroundColor Green
    [xml]$xmlElm = Get-Content -Path $xmlPath
    $xmlElm.CodeMetricsReport.Targets.Target.Assembly.Metrics.Metric
}

Read-Host -Prompt "Metrics calculation completed, you should be able to see results at '$metricsPath', press [ENTER] to exit."