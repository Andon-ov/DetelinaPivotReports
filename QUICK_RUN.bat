@echo off
setlocal enabledelayedexpansion
title Detelina Pivot Reports - Quick Run (.NET Framework 4.8)

echo ===============================================================================
echo     Detelina / Eltrade Pivot Reports - Quick Run (.NET Framework 4.8)
echo ===============================================================================
echo.

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"

set "PROJ_PATH=%ROOT%\DetelinaPivotReports\DetelinaPivotReports\DetelinaPivotReports.csproj"

set "DOTNET_BIN=dotnet"
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    if exist "%ProgramFiles%\dotnet\dotnet.exe" (
        set "DOTNET_BIN=%ProgramFiles%\dotnet\dotnet.exe"
    ) else if exist "C:\Program Files\dotnet\dotnet.exe" (
        set "DOTNET_BIN=C:\Program Files\dotnet\dotnet.exe"
    ) else (
        echo [ERROR] dotnet CLI was not found!
        pause
        exit /b 1
    )
)

echo Starting application via %DOTNET_BIN% run...
"%DOTNET_BIN%" run --project "%PROJ_PATH%"

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [!] An error occurred while running the application.
    pause
)
