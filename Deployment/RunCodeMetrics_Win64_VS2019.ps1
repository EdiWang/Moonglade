# .NET Code Metrics Script
# Author: Edi Wang
# Reference: https://docs.microsoft.com/en-us/visualstudio/code-quality/how-to-generate-code-metrics-data?view=vs-2019

param(
    $targetPath = "D:\GitHub\ediwang\Moonglade\src",
    $metricsPath = "D:\CodeMetricsResult"
)

Clear-Host
Write-Host "This script will run Code Metrics on [$targetPath], output to [$metricsPath]. Please confirm before continue." -ForegroundColor Green
Read-Host -Prompt "Press [ENTER] to continue, [CTRL + C] to cancel"

Import-Module "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\Microsoft.VisualStudio.DevShell.dll"; 
Enter-VsDevShell c436d3f6

Write-Host "Creating output directory" -ForegroundColor Yellow
New-Item -ItemType Directory -Force $metricsPath

Write-Host "Finding C# project files in '$targetPath'" -ForegroundColor Yellow
Get-ChildItem -Path $targetPath -Filter *.csproj -Recurse -File | ForEach-Object {
    $csprojPath = $_.FullName
    $projName = $_.Name
    $dirName = $_.DirectoryName

    if($csprojPath.EndsWith("Tests.csproj")) {
        Write-Host "Skipping Unit Test project '$projName'" -ForegroundColor Gray
    }
    else {
        Write-Host "Adding 'Microsoft.CodeAnalysis.Metrics' to '$projName'" 

        # TODO: figure out how to make 'Install-Package' refer to nuget not powershell...
        # cd $dirName
        # Install-Package Microsoft.CodeAnalysis.Metrics -Version 3.3.0

        $echo = dotnet add $csprojPath package Microsoft.CodeAnalysis.Metrics

        Write-Host "Running Code Metrics' for '$projName'" -ForegroundColor Cyan
        $echo = MSBuild $csprojPath /t:Metrics /p:MetricsOutputFile=$metricsPath/$projName.xml
    }
}

# MSBuild $targetPath /t:Metrics