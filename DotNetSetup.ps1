if (!([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) { Start-Process powershell.exe "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs; exit }

$testchoco = powershell choco -v
if (-not($testchoco)) {
    Write-Host ""
    Write-Host "Installing Chocolate for Windows..." -ForegroundColor Green
    Write-Host "------------------------------------" -ForegroundColor Green
    Set-ExecutionPolicy Bypass -Scope Process -Force; Invoke-Expression ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
}
else {
    Write-Host "Choco is already installed, skip installation."
}

$testdotnet = powershell dotnet --version
if (-not($testdotnet)) {
    Write-Host ""
    Write-Host "Installing .NET Core SDK..." -ForegroundColor Green
    Write-Host "------------------------------------" -ForegroundColor Green
    choco install dotnetcore-sdk -y
}
else {
    Write-Host ".NET Core SDK is already installed, checking new version..."

    if ($testdotnet.StartsWith("2.2")) {
        # Only update 2.2 SDK
        choco update dotnetcore-sdk -y
    }
}

Read-Host -Prompt "Setup is done, press [ENTER] to quit."