# How to Deploy Jcup Revit Tab

This tool supports Revit 2021, 2022, 2023, 2024, and 2025.

## Quick Install (For Coworkers)

1. **Unzip** the deployment folder provided to you.
2. Open the folder corresponding to your Revit version (e.g., `2024`).
3. Copy all files inside (`Jcup.dll`, `Jcup.addin`, etc.) to your Revit Addins folder:
   - **Path:** `%ProgramData%\Autodesk\Revit\Addins\2024\`
   *(Replace 2024 with your specific year)*

4. Restart Revit.

## How to Build New Versions (For Admin)

To build the tools for all versions at once:

1. Open `Revit Tab.sln` in Visual Studio 2022.
2. Select `Build` -> `Batch Build`.
3. Check all `Release_20xx` configurations.
4. Click **Build**.

Alternatively, run the included PowerShell script:
```powershell
.\build_and_package.ps1
```
This will create a `Deploy` folder with everything ready to zip.

## Troubleshooting

- If you don't see the tab: Check that `Jcup.addin` is in the correct `%ProgramData%` folder.
- If the tab crashes on load: Ensure you copied the `Jcup.dll` from the correct year folder (do not mix 2022 DLLs with 2024 Revit).
