# build_and_package.ps1

$scriptPath = $PSScriptRoot
$solutionPath = Join-Path $scriptPath "Revit Tab\Revit Tab.csproj"
$versions = @("2021", "2022", "2023", "2024", "2025")
$deployRoot = Join-Path $scriptPath "Deploy"

# 1. Clean Deploy Folder
if (Test-Path $deployRoot) { Remove-Item $deployRoot -Recurse -Force }
New-Item -Path $deployRoot -ItemType Directory | Out-Null

foreach ($ver in $versions) {
    Write-Host "Building for Revit $ver..." -ForegroundColor Cyan
    
    # 2. Build the project for this configuration
    # We use dotnet build but point to the full path
    dotnet build "$solutionPath" -c "Release_$ver" -v q
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed for $ver. Skipping." -ForegroundColor Red
        continue
    }

    # 3. Copy to Deploy folder
    $targetDir = Join-Path $deployRoot $ver
    New-Item -Path $targetDir -ItemType Directory | Out-Null
    
    # Construct path to the output bin folder
    # The csproj OutputPath is relative (bin\Release\202x\), so we combine it with project dir
    # SDK projects append the framework (net48) to the path
    $projectDir = Split-Path $solutionPath
    $sourceDir = Join-Path $projectDir "bin\Release\$ver\net48"
    
    # Fallback to check without net48 if not found (just in case)
    if (-not (Test-Path $sourceDir)) {
        $sourceDir = Join-Path $projectDir "bin\Release\$ver"
    }
    
    if (Test-Path $sourceDir) {
        Copy-Item "$sourceDir\*.dll" -Destination $targetDir
        
        # 4. Create .addin file
        $addinContent = @"
<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>Jcup Tab</Name>
    <Assembly>Jcup.dll</Assembly>
    <FullClassName>Revit_Tab.RevitApp</FullClassName>
    <ClientId>3B0CD4C4-D3D9-4FE7-96E1-0782C4F5B86D</ClientId>
    <VendorId>JCUP</VendorId>
    <VendorDescription>Jcup</VendorDescription>
  </AddIn>
</RevitAddIns>
"@
        
        $addinPath = Join-Path $targetDir "Jcup.addin"
        Set-Content -Path $addinPath -Value $addinContent
        
        Write-Host "Created $addinPath" -ForegroundColor Green
    } else {
        Write-Host "Warning: Output directory $sourceDir not found." -ForegroundColor Yellow
    }
}

Write-Host "Done! Check the '$deployRoot' folder." -ForegroundColor Yellow
