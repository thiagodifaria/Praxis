$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$infraPath = Join-Path $projectRoot "infra"

Push-Location $infraPath
docker compose --env-file .env build
Pop-Location
