if (!([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) { Start-Process powershell.exe "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs; exit }

function Check-Command($cmdname) {
    return [bool](Get-Command -Name $cmdname -ErrorAction SilentlyContinue)
}

if (Check-Command -cmdname 'choco') {
    Write-Host "Choco is already installed, skip installation."
}
else {
    Write-Host ""
    Write-Host "Installing Chocolate for Windows..." -ForegroundColor Green
    Write-Host "------------------------------------" -ForegroundColor Green
    Set-ExecutionPolicy Bypass -Scope Process -Force; Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
}

if (Check-Command -cmdname 'dotnet') {
    Write-Host ".NET Core SDK is already installed, checking new version..."
    $testdotnet = powershell dotnet --version
    if ($testdotnet.StartsWith("2.2")) {
        # Only update 2.2 SDK
        choco update dotnetcore-sdk -y
    }
}
else {
    Write-Host ""
    Write-Host "Installing .NET Core SDK..." -ForegroundColor Green
    Write-Host "------------------------------------" -ForegroundColor Green
    choco install dotnetcore-sdk -y
}

Read-Host -Prompt "Setup is done, press [ENTER] to quit."