param(
    [string]$MoongladeRoot = "",
    [string]$EditorRoot = "",
    [switch]$SkipEditorTests,
    [switch]$SkipMoongladeBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-ExistingDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "$Name directory was not found: $Path"
    }

    return (Resolve-Path -LiteralPath $Path).Path
}

function Run-Step {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Title,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    Write-Host ""
    Write-Host "==> $Title"
    & $Action
}

function Invoke-InDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Action
    )

    Push-Location -LiteralPath $Path
    try {
        & $Action
    }
    finally {
        Pop-Location
    }
}

function Get-RepositoryStatus {
    param([Parameter(Mandatory = $true)][string]$Path)

    Invoke-InDirectory $Path {
        git status --short
    }
}

$scriptDirectory = Resolve-Path -LiteralPath $PSScriptRoot
$skillDirectory = Split-Path -Parent $scriptDirectory.Path
$skillsDirectory = Split-Path -Parent $skillDirectory
$codexDirectory = Split-Path -Parent $skillsDirectory
$defaultMoongladeRoot = Split-Path -Parent $codexDirectory

if ([string]::IsNullOrWhiteSpace($MoongladeRoot)) {
    $MoongladeRoot = $defaultMoongladeRoot
}

$moongladePath = Resolve-ExistingDirectory -Path $MoongladeRoot -Name "Moonglade"

if ([string]::IsNullOrWhiteSpace($EditorRoot)) {
    $EditorRoot = Join-Path (Split-Path -Parent $moongladePath) "Moonglade.Editor"
}

$editorPath = Resolve-ExistingDirectory -Path $EditorRoot -Name "Moonglade.Editor"
$targetPath = Join-Path $moongladePath "src/Moonglade.Web/wwwroot/lib/moonglade-editor"
$targetPath = Resolve-ExistingDirectory -Path $targetPath -Name "Moonglade editor asset target"
$distPath = Join-Path $editorPath "dist"

$assetFiles = @(
    "moonglade-editor.js",
    "moonglade-editor.js.map",
    "moonglade-editor.css"
)

Run-Step "Initial repository status: Moonglade" {
    Get-RepositoryStatus $moongladePath
}

Run-Step "Initial repository status: Moonglade.Editor" {
    Get-RepositoryStatus $editorPath
}

if (-not $SkipEditorTests) {
    Run-Step "Run Moonglade.Editor tests" {
        Invoke-InDirectory $editorPath {
            npm test
        }
    }
}

Run-Step "Build Moonglade.Editor dist" {
    Invoke-InDirectory $editorPath {
        npm run build
    }
}

$distPath = Resolve-ExistingDirectory -Path $distPath -Name "Moonglade.Editor dist"

Run-Step "Copy ESM runtime assets into Moonglade" {
    foreach ($asset in $assetFiles) {
        $source = Join-Path $distPath $asset
        $target = Join-Path $targetPath $asset

        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
            throw "Expected built asset was not found: $source"
        }

        Copy-Item -LiteralPath $source -Destination $target -Force
        Write-Host "Copied $asset"
    }
}

Run-Step "Verify copied asset hashes" {
    foreach ($asset in $assetFiles) {
        $source = Join-Path $distPath $asset
        $target = Join-Path $targetPath $asset
        $sourceHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $source).Hash
        $targetHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $target).Hash

        if ($sourceHash -ne $targetHash) {
            throw "Hash mismatch for $asset"
        }

        Write-Host "$asset $sourceHash"
    }
}

Run-Step "List Moonglade editor asset directory" {
    Get-ChildItem -LiteralPath $targetPath | Select-Object Name, Length
}

if (-not $SkipMoongladeBuild) {
    Run-Step "Build Moonglade Web project" {
        Invoke-InDirectory $moongladePath {
            dotnet build "src/Moonglade.Web/Moonglade.Web.csproj"
        }
    }
}

Run-Step "Final repository status: Moonglade" {
    Get-RepositoryStatus $moongladePath
}

Write-Host ""
Write-Host "Moonglade.Editor asset sync completed."
