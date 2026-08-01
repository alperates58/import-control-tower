$ErrorActionPreference = "Stop"

Write-Host "=== PHASES 01-04 LIVE ENDPOINT VERIFICATION MATRIX ===" -ForegroundColor Cyan

# 1. Login
$loginBody = @{
    usernameOrEmail = "admin@controltower.local"
    password = "AdminSecurePassword123!"
} | ConvertTo-Json

$loginRes = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginRes.accessToken
$headers = @{
    "Authorization" = "Bearer $token"
    "X-CSRF-TOKEN" = "1"
}

Write-Host "[✓] Authentication Token Acquired" -ForegroundColor Green

# 2. Get Documents List
$docsRes = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/documents" -Method Get -Headers $headers
Write-Host "[✓] GET /api/v1/documents - Count: $($docsRes.Count)" -ForegroundColor Green

# 3. Create Sample Case for Document Upload
$idemKey = [Guid]::NewGuid().ToString()
$caseHeaders = $headers.Clone()
$caseHeaders.Add("Idempotency-Key", $idemKey)
$caseBody = @{
    title = "Live Verification Phase04 Case"
    supplierName = "Global Logistics Ltd"
    defaultTransportMode = "Sea"
    originCountry = "CN"
    incoterm = "FOB"
} | ConvertTo-Json

$caseRes = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/import-cases" -Method Post -Headers $caseHeaders -Body $caseBody -ContentType "application/json"
$caseId = $caseRes.id
Write-Host "[✓] POST /api/v1/import-cases Created Case: $caseId ($($caseRes.caseNumber))" -ForegroundColor Green

# 4. Check Checklist
$checklistRes = Invoke-RestMethod -Uri "http://localhost:8080/api/v1/import-cases/$caseId/document-checklist" -Method Get -Headers $headers
Write-Host "[✓] GET /api/v1/import-cases/$caseId/document-checklist - Status: $($checklistRes.status), Required: $($checklistRes.totalRequiredCount), Missing: $($checklistRes.missingCount)" -ForegroundColor Green

Write-Host "=== ALL PHASES 01-04 LIVE VERIFICATION TESTS PASSED SUCCESSFULLY ===" -ForegroundColor Green
