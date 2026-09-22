param(
    [string]$BaseUrl = "http://localhost:5000",
    [string]$ExternalId = "TEST-$([DateTime]::Now.ToString('yyyyMMddHHmmss'))"
)

$body = @{
    externalId = $ExternalId
    alarmTime = (Get-Date).ToString("o")
    keyword = "FEU"
    message = "Testalarm aus PowerShell"
    street = "Musterstraße"
    houseNumber = "17"
    city = "Musterstadt"
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/alarm" -ContentType "application/json" -Body $body
