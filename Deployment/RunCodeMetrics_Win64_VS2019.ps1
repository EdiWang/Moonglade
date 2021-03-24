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

        # Check hash to prevent downloading malware
        Write-Host "Checking file hash..." -ForegroundColor Cyan
        $zipHash = "F2A61A34E9913BB1A66101E092D8755B2C9AFA8ED804A92EC0450E44F92BC82E"
        $downloadedZipHash = Get-FileHash .\Metrics.zip
        Write-Host "SHA256:" $downloadedZipHash.Hash

        if ($downloadedZipHash.Hash -eq $zipHash) {
            Expand-Archive -Path Metrics.zip -DestinationPath $metricsExePath.Replace("Metrics\Release\net472", "") -Force    
        }
        else {
            Write-Host "Metrics.zip hash does not match '$zipHash', script is terminated to prevent security breach"
            exit
        }
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

Write-Host "Finding C# project files in '$targetPath'" -ForegroundColor Yellow
Get-ChildItem -Path $targetPath -Filter *.csproj -Recurse -File | ForEach-Object {
    $csprojPath = $_.FullName
    $projName = $_.Name
    #$dirName = $_.DirectoryName

    if ($csprojPath.EndsWith("Tests.csproj")) {
        Write-Host "Skipped Unit Test project '$projName'" -ForegroundColor Gray
    }
    else {
        Write-Host "Running Code Metrics' for '$projName'" -ForegroundColor Cyan
        if ($useMetricsExe) {
            Set-Location $metricsExePath
            $echo = .\Metrics.exe /project:$csprojPath /out:$metricsPath/$projName.xml
        }
        else {
            Write-Host "Adding 'Microsoft.CodeAnalysis.Metrics' to '$projName'" 

            $echo = dotnet add $csprojPath package Microsoft.CodeAnalysis.Metrics
            $echo = MSBuild $csprojPath /t:Metrics /p:MetricsOutputFile=$metricsPath/$projName.xml
        }
    }
}

$collectionWithItems = New-Object System.Collections.ArrayList
$reports = Get-ChildItem -Path $metricsPath -Filter *.csproj.xml -Recurse -File
foreach ($report in $reports) {
    $xmlPath = $report.FullName
    $xmlName = $report.Name

    Write-Host "Code Metrics for '$xmlName'" -ForegroundColor Green
    Write-Host "---------------------------------------------------"
    [xml]$xmlElm = Get-Content -Path $xmlPath
    $xmlElm.CodeMetricsReport.Targets.Target.Assembly.Metrics.Metric | ForEach-Object {
        $name = $_.Name
        $val = $_.Value
        Write-Host "$name [$val]"

        $temp = New-Object System.Object
        $temp | Add-Member -MemberType NoteProperty -Name "Project" -Value $xmlName.Replace(".csproj.xml", "")
        $temp | Add-Member -MemberType NoteProperty -Name "Name" -Value $name
        $temp | Add-Member -MemberType NoteProperty -Name "Value" -Value $val
        $collectionWithItems.Add($temp) | Out-Null
    }
    Write-Host
}

Write-Host "Exporting to CSV file" -ForegroundColor Cyan
$collectionWithItems | Export-Csv -Path $metricsPath\CodeMetrics.csv -NoTypeInformation

Write-Host "Exporting to HTML file" -ForegroundColor Cyan
$collectionWithItems | ConvertTo-Html | Out-File $metricsPath\CodeMetrics.html

Write-Host
Write-Host "Metrics calculation completed, you should be able to see results at '$metricsPath'." -ForegroundColor Green
Set-Location $metricsPath