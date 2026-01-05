<#
.SYNOPSIS
    Builds the Revit Tab project and deploys it to the local Revit Addins folders for specified versions.

.DESCRIPTION
    1. Builds the solution in Release mode.
    2. Copies the build artifacts (DLLs) to %AppData%\Autodesk\Revit\Addins\<Year>\ClancyTheys\.
    3. Creates/Updates the .addin manifest file for each version to point to the new DLL.

.NOTES
    Ensure that Revit is closed before running this script to avoid file locking issues.
#>

$SolutionPath = Join-Path $PSScriptRoot "Revit Tab.sln"
$ProjectFolder = Join-Path $PSScriptRoot "Revit Tab"
$BuildOutput = Join-Path $ProjectFolder "bin\Release"
$TargetVersions = @("2022", "2023", "2024")

# 1. Build the Project
Write-Host "Restoring and Building Project..." -ForegroundColor Cyan
dotnet restore $SolutionPath
dotnet build $SolutionPath -c Release

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed. Fix build errors before deploying."
    exit
}

if (-not (Test-Path $BuildOutput)) {
    Write-Error "Build output not found at $BuildOutput"
    exit
}

# 2. Deploy to each version
foreach ($Year in $TargetVersions) {
    Write-Host "`nDeploying to Revit $Year..." -ForegroundColor Yellow
    
    # Define paths
    $AddinsFolder = "$env:AppData\Autodesk\Revit\Addins\$Year"
    $PluginFolder = Join-Path $AddinsFolder "ClancyTheys"
    $AddinFile = Join-Path $AddinsFolder "ClancyTheys.addin"

    # Ensure Addins folder exists (skip if Revit version not installed/folder missing?)
    # Actually, we should probably create it if it doesn't exist, or warn.
    if (-not (Test-Path $AddinsFolder)) {
        New-Item -Path $AddinsFolder -ItemType Directory -Force | Out-Null
    }

    # Prepare Plugin Folder
    if (-not (Test-Path $PluginFolder)) {
        New-Item -Path $PluginFolder -ItemType Directory -Force | Out-Null
    }

    # Copy Files
    try {
        Copy-Item "$BuildOutput\*" -Destination $PluginFolder -Recurse -Force
        Write-Host "  [OK] Copied binaries to $PluginFolder" -ForegroundColor Green
    }
    catch {
        Write-Error "  [FAIL] Failed to copy files. Is Revit open?"
        continue
    }

    # Remove any nested .addin files that may point to old DLL paths.
    # Revit can load .addin manifests from subfolders, which can cause it to run an outdated build.
    Get-ChildItem -Path $PluginFolder -Recurse -Filter "*.addin" -ErrorAction SilentlyContinue |
        Remove-Item -Force -ErrorAction SilentlyContinue

    # Create .addin manifest
    $DllPath = Join-Path $PluginFolder "Jcup.dll"
    
    $AddinContent = @"
<?xml version="1.0" encoding="utf-8" standalone="no"?>
<RevitAddIns>
  <AddIn Type="Application">
    <Name>Clancy Theys</Name>
    <Assembly>$DllPath</Assembly>
    <AddInId>87654321-4321-4321-4321-1234567890ab</AddInId>
    <FullClassName>Revit_Tab.RevitApp</FullClassName>
    <VendorId>ClancyTheys</VendorId>
    <VendorDescription>Clancy Theys Revit Tools</VendorDescription>
  </AddIn>
</RevitAddIns>
"@

    Set-Content -Path $AddinFile -Value $AddinContent -Encoding UTF8
    Write-Host "  [OK] Created manifest at $AddinFile" -ForegroundColor Green
}

Write-Host "`nDeployment Complete!" -ForegroundColor Cyan
