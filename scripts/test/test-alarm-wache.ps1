param([string]$BaseUrl = "http://localhost:5080")
$body = @{
    externalId = "TEST-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    alarmTime = (Get-Date).ToString("o")
    keyword = "FEU"
    message = "Testalarm Gebäudebrand"
    street = "Musterstraße"
    houseNumber = "12"
    city = "Musterstadt"
} | ConvertTo-Json
$bytes = [Text.Encoding]::UTF8.GetBytes($body)
Invoke-RestMethod -Uri "$BaseUrl/api/alarm" -Method Post -ContentType "application/json; charset=utf-8" -Body $bytes
