param(
    [string]$Url = "http://localhost:44331",
    [int]$TimeoutSeconds = 120,
    [int]$PollIntervalSeconds = 2
)

$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
$lastError = $null

Write-Host "Waiting for mmria.services at $Url ..."

while ((Get-Date) -lt $deadline) {
    try {
        $response = Invoke-WebRequest -Uri $Url -Method Get -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
            Write-Host "mmria.services is responding at $Url."
            exit 0
        }
    }
    catch {
        $lastError = $_.Exception.Message
    }

    Start-Sleep -Seconds $PollIntervalSeconds
}

if ($lastError) {
    Write-Error "Timed out waiting for mmria.services at $Url. Last error: $lastError"
}
else {
    Write-Error "Timed out waiting for mmria.services at $Url."
}

exit 1
