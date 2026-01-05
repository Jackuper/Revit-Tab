# Revit Tab (Clancy Theys)

A custom Revit Add-in that provides productivity tools for project setup and modeling.

## Features

### 1. Create Sheets
*   **Location:** `Clancy Theys` Tab > `Project Setup` Panel > `Create Sheets`
*   **Function:** Batch creates sheets based on user input (Sheet Number, Name, and Quantity).
*   **Usage:**
    1.  Click the button.
    2.  Enter the starting Sheet Number (e.g., `A101`).
    3.  Enter the Sheet Name.
    4.  Enter the Quantity of sheets to generate.
    5.  The tool automatically increments the numeric portion of the sheet number.

### 2. Create King Studs
*   **Location:** `Clancy Theys` Tab > `Project Setup` Panel > `Create King Studs`
*   **Function:** Automatically places structural stud families around Doors and Windows.
*   **Advanced:** Detects openings in **linked models** and places the studs in the host model at the correct coordinates.
*   **Dependencies:** Requires a `Stud.rfa` family in the `Families/` folder (included in build).

### 3. 3D Per Level
*   **Location:** `Clancy Theys` Tab > `Project Setup` Panel > `3D Per Level`
*   **Function:** Automatically creates an isometric 3D view for each level in the project, with the section box cropped to that level's height.

## Development Setup

### Prerequisites
*   Visual Studio 2022 (or compatible)
*   .NET Framework 4.8 (for Revit 2023/2024)
*   .NET 8 SDK (for Revit 2025)
*   Autodesk Revit 2023, 2024, or 2025 (for testing/debugging)

### Build & Deploy
This project uses **NuGet** for Revit API references and **multi-targeting** to support multiple Revit versions.

1.  Open `Revit Tab.csproj` in Visual Studio.
2.  Build the solution (`Ctrl+Shift+B`).
3.  **Automatic Deployment:** The project includes a Post-Build event that automatically copies the Add-in manifest and DLLs to your Revit Addins folders based on the target framework:
    *   **Net48 Build:** Deploys to `%AppData%\Autodesk\Revit\Addins\2023\` and `2024\`
    *   **Net8.0 Build:** Deploys to `%AppData%\Autodesk\Revit\Addins\2025\`

### Debugging
1.  Set the project to "Start external program" in Debug properties.
2.  Choose the Revit executable version you want to debug (e.g., `C:\Program Files\Autodesk\Revit 2025\Revit.exe`).
3.  Press F5 to start Revit with the debugger attached.
