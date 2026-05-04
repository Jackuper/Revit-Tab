# Clean and Deploy Revit Tab Add-in
# This script cleans old files and deploys fresh builds to Revit

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Clean and Deploy Revit Tab" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan

$revitVersions = @("2023", "2024", "2025")
$addinsBase = "$env:APPDATA\Autodesk\Revit\Addins"
$projectPath = $PSScriptRoot

foreach ($version in $revitVersions) {
    $addinsPath = "$addinsBase\$version"
    $addinFile = "$addinsPath\MyCustomTab.addin"
    $dllFolder = "$addinsPath\Jcup"

    Write-Host "`nProcessing Revit $version..." -ForegroundColor Yellow

    # Remove old .addin file
    if (Test-Path $addinFile) {
        Write-Host "  Removing old .addin file..." -ForegroundColor Gray
        Remove-Item $addinFile -Force
    }

    # Remove old DLL folder
    if (Test-Path $dllFolder) {
        Write-Host "  Removing old DLL folder..." -ForegroundColor Gray
        Remove-Item $dllFolder -Recurse -Force
    }

    # Determine which build to deploy
    if ($version -eq "2025") {
        $sourceDll = "$projectPath\bin\Debug\net8.0-windows"
    } else {
        $sourceDll = "$projectPath\bin\Debug\net48"
    }

    # Check if build exists
    if (-not (Test-Path "$sourceDll\Jcup.dll")) {
        Write-Host "  Build not found at $sourceDll" -ForegroundColor Red
        Write-Host "  Please rebuild the project first!" -ForegroundColor Red
        continue
    }

    # Create new DLL folder
    Write-Host "  Creating DLL folder..." -ForegroundColor Gray
    New-Item -Path $dllFolder -ItemType Directory -Force | Out-Null

    # Copy all DLL files
    Write-Host "  Copying DLLs from $sourceDll..." -ForegroundColor Gray
    Copy-Item "$sourceDll\*" -Destination $dllFolder -Recurse -Force

    # Copy .addin file
    Write-Host "  Copying .addin file..." -ForegroundColor Gray
    Copy-Item "$projectPath\Utility\MyCustomTab.addin" -Destination $addinFile -Force

    Write-Host "  Deployed successfully!" -ForegroundColor Green
}

Write-Host "`n======================================" -ForegroundColor Cyan
Write-Host "Deployment Complete!" -ForegroundColor Green
Write-Host "Close and reopen Revit to load the new version." -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
