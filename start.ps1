#Requires -Version 5.1

$ErrorActionPreference = 'Stop'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET 10 SDK is required. Install it and ensure dotnet is available on PATH.'
}

Push-Location -LiteralPath $PSScriptRoot
try {
    $project = Join-Path $PSScriptRoot 'src\Moonglade.Web\Moonglade.Web.csproj'
    & dotnet run --project $project --launch-profile 'Moonglade.Web'
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

exit $exitCode
