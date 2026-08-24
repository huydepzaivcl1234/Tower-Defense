$ErrorActionPreference = 'Stop'

$ProjectRoot = Split-Path -Parent $PSScriptRoot
$Generator = Join-Path $ProjectRoot 'Assets\Art\Blender\MainBaseGenerator.py'

if (-not (Test-Path $Generator)) {
    throw "MainBaseGenerator.py not found: $Generator"
}

$Candidates = @()
$Roots = @(
    'C:\Program Files\Blender Foundation',
    'C:\Program Files (x86)\Blender Foundation'
)

foreach ($Root in $Roots) {
    if (-not (Test-Path $Root)) { continue }
    $Candidates += Get-ChildItem -Path $Root -Recurse -Filter blender.exe -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty FullName
}

if (-not $Candidates -or $Candidates.Count -eq 0) {
    $cmd = Get-Command blender.exe -ErrorAction SilentlyContinue
    if ($cmd) { $Candidates += $cmd.Source }
}

if (-not $Candidates -or $Candidates.Count -eq 0) {
    throw 'Blender was not found. Install Blender or add blender.exe to PATH.'
}

# Prefer the newest installed Blender folder/version.
$Blender = $Candidates | Sort-Object -Descending | Select-Object -First 1
Write-Host "Using Blender: $Blender" -ForegroundColor Cyan
Write-Host "Building Main Base..." -ForegroundColor Cyan

& $Blender --background --python $Generator
if ($LASTEXITCODE -ne 0) {
    throw "Blender failed with exit code $LASTEXITCODE"
}

$Fbx = Join-Path $ProjectRoot 'Assets\Models\MainBase\MainBase.fbx'
$Blend = Join-Path $ProjectRoot 'Assets\Art\Blender\MainBase.blend'

if (-not (Test-Path $Fbx)) {
    throw "Build finished but FBX was not created: $Fbx"
}

Write-Host ''
Write-Host 'Main Base build complete.' -ForegroundColor Green
Write-Host "FBX:   $Fbx"
Write-Host "Blend: $Blend"
Write-Host ''
Write-Host 'Return to Unity, wait for import, then use:' -ForegroundColor Yellow
Write-Host 'Tower Defense -> Art -> Setup Main Base Model' -ForegroundColor Yellow
