param(
    [string]$BaseUrl = "http://localhost:5080",
    [string]$ExternalId = ("FE2-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
)

# Simuliert einen produktiven FE2-Aufruf über das Plugin "URL öffnen".
$params = @{
    externalId  = $ExternalId
    keyword     = "F2"
    message     = "Zimmerbrand - FE2 Integrationstest"
    street      = "Hauptstraße"
    houseNumber = "12"
    city        = "Musterstadt"
    alarmTime   = (Get-Date).ToString("o")
}

$query = ($params.GetEnumerator() | ForEach-Object {
    [uri]::EscapeDataString($_.Key) + "=" + [uri]::EscapeDataString([string]$_.Value)
}) -join "&"

$url = "$BaseUrl/api/alarm/fe2?$query"
Write-Host "GET $url"
Invoke-RestMethod -Uri $url -Method Get | ConvertTo-Json -Depth 5
