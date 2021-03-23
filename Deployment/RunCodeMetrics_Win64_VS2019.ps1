# .NET Code Metrics Script
# Author: Edi Wang
# Reference: https://docs.microsoft.com/en-us/visualstudio/code-quality/how-to-generate-code-metrics-data?view=vs-2019

param(
    $targetPath = "D:\GitHub\ediwang\Moonglade\src",
    $metricsPath = "D:\CodeMetricsResult",
    [bool] $useMetricsExe = 1,
    $metricsExePath = "D:\Workspace\Metrics\Release\net472",
    $devPsPath = "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\Microsoft.VisualStudio.DevShell.dll"
)

Clear-Host
Write-Host "This script will run Code Metrics on [$targetPath], output to [$metricsPath]. Please confirm before continue." -ForegroundColor Green
if ($useMetricsExe) {
    Write-Host "+ Using pre-compiled Metrics.exe at 'metricsExePath'" -ForegroundColor Cyan
}
else {
    Write-Host "+ Using 'Microsoft.CodeAnalysis.Metrics', this will only support .NET Core or .NET 5 projects." -ForegroundColor Cyan
}

Read-Host -Prompt "Press [ENTER] to continue, [CTRL + C] to cancel"

Import-Module $devPsPath; 
Enter-VsDevShell c436d3f6

Write-Host "Creating output directory" -ForegroundColor Yellow
New-Item -ItemType Directory -Force $metricsPath

cd $targetPath
Write-Host "Finding C# project files in '$targetPath'" -ForegroundColor Yellow
Get-ChildItem -Path $targetPath -Filter *.csproj -Recurse -File | ForEach-Object {
    $csprojPath = $_.FullName
    $projName = $_.Name
    $dirName = $_.DirectoryName

    if ($csprojPath.EndsWith("Tests.csproj")) {
        Write-Host "Skipping Unit Test project '$projName'" -ForegroundColor Gray
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

cd $targetPath
Read-Host -Prompt "Metrics calculation completed, you should be able to see results at '$metricsPath', press [ENTER] to exit."