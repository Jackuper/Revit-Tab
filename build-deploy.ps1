# ===========================
# CONFIG
# ===========================
$Versions = @("2022", "2023", "2024")
$SourceFolder = "BuildOutput"
$DeployFolder = "C:\RevitAddins\ClancyTheys"

# Ensure deploy folders exist
foreach ($v in $Versions) {
    $targetPath = Join-Path $DeployFolder $v
    if (-not (Test-Path $targetPath)) {
        New-Item -ItemType Directory -Path $targetPath | Out-Null
    }
}

# Deploy each version
foreach ($v in $Versions) {
    $dllName = "Revit_Tab_$v.dll"
    $sourceDll = Join-Path "$SourceFolder\$v" $dllName
    $targetDll = Join-Path "$DeployFolder\$v" $dllName

    Write-Host "`n[INFO] Looking for: $sourceDll"

    if (Test-Path $sourceDll) {
        Copy-Item -Path $sourceDll -Destination $targetDll -Force
        Write-Host "[✅] Deployed to $targetDll" -ForegroundColor Green
    } else {
        Write-Warning "[⚠️] Missing DLL for $v at $sourceDll"
    }
}

Write-Host "`n[INFO] Deploy complete." -ForegroundColor Yellow
pause

