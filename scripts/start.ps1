$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$infraPath = Join-Path $projectRoot "infra"
$waitScript = Join-Path $PSScriptRoot "wait-for-services.ps1"

Push-Location $infraPath
docker compose --env-file .env up -d --build
Pop-Location

& $waitScript
