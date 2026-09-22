param(
    [string]$BaseUrl = "http://localhost:5080"
)

# Simuliert, was das FE2-Plugin "URL öffnen" an den Diagnose-Endpunkt sendet.
$params = @{
    externalId  = "FE2-DEBUG-" + (Get-Date -Format "yyyyMMdd-HHmmss")
    keyword     = "F2"
    message     = "Zimmerbrand - FE2 Debugtest"
    street      = "Hauptstraße"
    houseNumber = "12"
    city        = "Musterstadt"
    alarmTime   = (Get-Date).ToString("o")
}

$query = ($params.GetEnumerator() | ForEach-Object {
    [uri]::EscapeDataString($_.Key) + "=" + [uri]::EscapeDataString([string]$_.Value)
}) -join "&"

$url = "$BaseUrl/api/alarm/fe2/debug?$query"
Write-Host "GET $url"
Invoke-RestMethod -Uri $url -Method Get | ConvertTo-Json -Depth 5
