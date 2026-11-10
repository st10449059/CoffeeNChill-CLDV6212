$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$compose = Join-Path $root "docker-compose.part3.yml"
$network = "coffeenchill-part3_coffee_backend"

if (-not (Test-Path $compose)) {
    throw "Part 3 Compose file not found."
}

Write-Host "Checking Part 3 Docker containers..."

$functionsId = docker compose -f $compose ps -q functions

if ($LASTEXITCODE -ne 0 -or
    [string]::IsNullOrWhiteSpace($functionsId)) {
    throw "Functions container is not running."
}

$azuriteId = docker compose -f $compose ps -q azurite

if ($LASTEXITCODE -ne 0 -or
    [string]::IsNullOrWhiteSpace($azuriteId)) {
    throw "Azurite container is not running."
}

$functionsRunning = docker inspect `
    --format '{{.State.Running}}' $functionsId

$azuriteHealth = docker inspect `
    --format '{{.State.Health.Status}}' $azuriteId

if ($functionsRunning -ne "true") {
    throw "Functions container is not running."
}

if ($azuriteHealth -ne "healthy") {
    throw "Azurite is not healthy."
}

Write-Host "PASS: Containers running and storage healthy."

$functionsPorts = docker port $functionsId
$azuritePorts = docker port $azuriteId

if (-not [string]::IsNullOrWhiteSpace(
        ($functionsPorts | Out-String)) -or
    -not [string]::IsNullOrWhiteSpace(
        ($azuritePorts | Out-String))) {
    throw "A backend container has published ports."
}

Write-Host "PASS: Backend ports are not published."

$menuStatus = docker run --rm `
    --network $network `
    curlimages/curl:8.16.0 `
    --silent --show-error `
    --output /dev/null `
    --write-out '%{http_code}' `
    --max-time 15 `
    http://functions/api/menu

if ($LASTEXITCODE -ne 0 -or $menuStatus -ne "200") {
    throw "Menu endpoint failed. HTTP: $menuStatus"
}

Write-Host "PASS: Internal menu endpoint returned HTTP 200."

$validationStatus = docker run --rm `
    --network $network `
    curlimages/curl:8.16.0 `
    --silent --show-error `
    --output /dev/null `
    --write-out '%{http_code}' `
    --max-time 15 `
    --request PUT `
    --header "Content-Type: application/json" `
    --data "{}" `
    http://functions/api/orders/status

if ($LASTEXITCODE -ne 0 -or $validationStatus -ne "400") {
    throw "Order validation failed. HTTP: $validationStatus"
}

Write-Host "PASS: Invalid order request returned HTTP 400."
Write-Host "ALL PART 3 DOCKER SMOKE TESTS PASSED."
