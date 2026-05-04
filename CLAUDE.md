# Revit-Tab Project Context

## Overview
Custom Revit add-in for **Clancy Theys** providing automation tools for construction project setup. Assembly name is `Jcup.dll`, appears as "Clancy Theys" tab in Revit.

## Quick Commands
```bash
# Build (Debug for Revit 2023)
dotnet build Revit-Tab.csproj

# Build all release versions
powershell -File build_and_package.ps1

# Clean and deploy locally
powershell -File Clean-And-Deploy.ps1

# Deploy to local Revit addins folders
powershell -File Deploy-Local.ps1
```

## Project Structure
```
Revit-Tab/                       # Root project folder
├── Source/
│   ├── RevitApp.cs              # Main entry point, ribbon UI setup
│   └── Commands/                # Command implementations
├── Page Creation/               # Sheet creation feature + WPF dialog
├── KingStuds/                   # Structural stud placement feature
├── Families/                    # Embedded .rfa family files
├── Images/                      # Button icons (PNG, 32px)
├── Utility/
│   └── MyCustomTab.addin        # Add-in manifest
└── Revit-Tab.csproj             # Project file
```

## Current Features
1. **Create Sheets** - Batch create sheets with auto-incrementing numbers
2. **King Studs** - Place structural studs around door/window openings (supports linked models)
3. **3D Per Level** - Generate isometric 3D views with section boxes per level

## Target Frameworks
- Revit 2021-2024: .NET Framework 4.8
- Revit 2025: .NET 8.0 (net8.0-windows)

## Coding Patterns
- Each command implements `IExternalCommand`
- Use `TransactionMode.Manual` with explicit `Transaction` blocks
- Namespace: `Revit_Tab`
- Error handling: Try-catch with `TaskDialog` for user feedback
- WPF dialogs: Call `.Freeze()` on resources for Revit compatibility

## Adding a New Command
1. Create class implementing `IExternalCommand` with `[Transaction(TransactionMode.Manual)]`
2. Add button in `RevitApp.cs` `OnStartup()` method
3. Add icon to `Images/` folder (32x32 PNG recommended)
4. Set icon Build Action to "Embedded Resource"

## Key APIs Used
- `Autodesk.Revit.DB` - Core database/geometry
- `Autodesk.Revit.UI` - Ribbon, dialogs, commands
- `Autodesk.Revit.DB.Structure` - Structural elements
- `RevitLinkInstance` - Working with linked models

## Deployment Paths
- Manifest: `%AppData%\Autodesk\Revit\Addins\{YEAR}\MyCustomTab.addin`
- DLL folder: `%AppData%\Autodesk\Revit\Addins\{YEAR}\Jcup\`

## GitHub
- Repo: https://github.com/Jackuper/Revit-Tab
- CI/CD: GitHub Actions builds all versions on push to main

## Notes
- Aspose.Cells is referenced but not actively used yet
- Linked model support requires coordinate transforms (see KingStudsCommand)
- Section boxes must have `IsSectionBoxActive = true` to display correctly
