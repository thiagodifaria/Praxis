$ErrorActionPreference = "Stop"

$apiHealth = "http://localhost:8080/health/ready"
$maxAttempts = 60

for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
    try {
        $response = Invoke-WebRequest -Uri $apiHealth -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            Write-Host "Praxis API pronta em $apiHealth"
            exit 0
        }
    }
    catch {
        Start-Sleep -Seconds 2
    }
}

throw "A API nao ficou pronta dentro do tempo esperado."
