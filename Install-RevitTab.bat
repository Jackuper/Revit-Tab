@echo off
setlocal EnableDelayedExpansion

echo ========================================================
echo      Installing Jcup Revit Tab for All Versions
echo ========================================================
echo.

:: Set the source directory to where this script is running
set "SCRIPT_DIR=%~dp0"
set "DEPLOY_ROOT=%SCRIPT_DIR%Deploy"

:: check if Deploy folder exists
if not exist "%DEPLOY_ROOT%" (
    echo [ERROR] Could not find the 'Deploy' folder.
    echo Please make sure you unzipped the entire package.
    echo Expected: %DEPLOY_ROOT%
    pause
    exit /b
)

:: Loop through supported years
for %%Y in (2021 2022 2023 2024 2025) do (
    set "TARGET_DIR=%ProgramData%\Autodesk\Revit\Addins\%%Y"
    set "SOURCE_DIR=%DEPLOY_ROOT%\%%Y"
    
    :: Check if this Revit version is installed (by checking if Addins folder exists or could be created)
    if exist "%ProgramData%\Autodesk\Revit\Addins\%%Y" (
        echo [INFO] Found Revit %%Y installed.
        
        if exist "!SOURCE_DIR!" (
            echo        Installing add-in for Revit %%Y...
            xcopy /E /I /Y "!SOURCE_DIR!\*" "!TARGET_DIR!\" >nul
            if !errorlevel! equ 0 (
                echo        [SUCCESS] Installed to !TARGET_DIR!
            ) else (
                echo        [ERROR] Failed to copy files. You might need to run as Administrator.
            )
        ) else (
            echo        [WARNING] No build files found for version %%Y in Deploy folder. Skipping.
        )
    ) else (
        :: echo        [INFO] Revit %%Y not found on this machine. Skipping.
    )
)

echo.
echo ========================================================
echo                  Installation Complete
echo ========================================================
echo Please restart Revit to see the changes.
pause
