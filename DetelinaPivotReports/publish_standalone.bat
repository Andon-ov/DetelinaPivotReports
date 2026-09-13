@echo off
echo =====================================================================
echo   Building DetelinaPivotReports (.NET Framework 4.8 Windows)
echo =====================================================================
echo.

cd /d "%~dp0"

echo [1/3] Cleaning previous publish folder...
rmdir /s /q publish 2>nul

echo [2/3] Running dotnet build (Release, .NET Framework 4.8)...
dotnet build DetelinaPivotReports\DetelinaPivotReports.csproj -c Release -o .\publish

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERROR] Build failed!
    pause
    exit /b 1
)

echo.
echo [3/3] Verifying appsettings.json in publish folder...
if not exist ".\publish\appsettings.json" (
    copy ".\DetelinaPivotReports\appsettings.json" ".\publish\appsettings.json" >nul
)

echo.
echo =====================================================================
echo  SUCCESS! Application built in folder: %~dp0publish\
echo  Executable: DetelinaPivotReports.exe (.NET Framework 4.8)
echo =====================================================================
pause
