@echo off
setlocal enabledelayedexpansion
title Detelina Pivot Reports - Launcher

echo ===============================================================================
echo     Detelina / Eltrade Pivot Reports (.NET Framework 4.8 WPF Desktop)
echo ===============================================================================
echo.

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"

set "PROJ_PATH=%ROOT%\DetelinaPivotReports\DetelinaPivotReports\DetelinaPivotReports.csproj"
set "EXE_PATH=%ROOT%\DetelinaPivotReports.exe"
set "PUBLISH_DIR=%ROOT%\publish"
set "SETTINGS_SRC=%ROOT%\DetelinaPivotReports\DetelinaPivotReports\appsettings.json"
set "SETTINGS_DEST=%ROOT%\appsettings.json"

:: Check if compiled exe already exists
if exist "%EXE_PATH%" (
    echo [OK] Found existing DetelinaPivotReports.exe!
    echo.
    echo 1 - Launch application (DetelinaPivotReports.exe)
    echo 2 - Rebuild application (.NET Framework 4.8 Rebuild)
    echo 3 - Exit
    echo.
    set /p USER_CHOICE="Please enter your choice [1, 2 or 3]: "
    if "!USER_CHOICE!"=="1" goto :RUN_APP
    if "!USER_CHOICE!"=="3" exit /b 0
)

:FIND_BUILD_TOOL
echo [1/3] Searching for build tools (dotnet / msbuild)...
set "BUILD_TOOL=dotnet"
set "IS_MSBUILD=0"

where dotnet >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo     Found in system PATH: dotnet
    goto :CHECK_PROJECT
)

if exist "%ProgramFiles%\dotnet\dotnet.exe" (
    set "BUILD_TOOL=%ProgramFiles%\dotnet\dotnet.exe"
    echo     Found: %ProgramFiles%\dotnet\dotnet.exe
    goto :CHECK_PROJECT
)

if exist "C:\Program Files\dotnet\dotnet.exe" (
    set "BUILD_TOOL=C:\Program Files\dotnet\dotnet.exe"
    echo     Found: C:\Program Files\dotnet\dotnet.exe
    goto :CHECK_PROJECT
)

if exist "%SystemDrive%\Program Files\dotnet\dotnet.exe" (
    set "BUILD_TOOL=%SystemDrive%\Program Files\dotnet\dotnet.exe"
    goto :CHECK_PROJECT
)

if exist "%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe" (
    set "BUILD_TOOL=%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe"
    goto :CHECK_PROJECT
)

:: Check for MSBuild if dotnet not found
where msbuild >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    set "BUILD_TOOL=msbuild"
    set "IS_MSBUILD=1"
    echo     Found in system PATH: msbuild
    goto :CHECK_PROJECT
)

:: Check Visual Studio 2022 / 2019 MSBuild paths
for %%v in (2022 2019) do (
    for %%e in (Enterprise Professional Community BuildTools) do (
        if exist "%ProgramFiles%\Microsoft Visual Studio\%%v\%%e\MSBuild\Current\Bin\MSBuild.exe" (
            set "BUILD_TOOL=%ProgramFiles%\Microsoft Visual Studio\%%v\%%e\MSBuild\Current\Bin\MSBuild.exe"
            set "IS_MSBUILD=1"
            echo     Found MSBuild: !BUILD_TOOL!
            goto :CHECK_PROJECT
        )
        if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\%%v\%%e\MSBuild\Current\Bin\MSBuild.exe" (
            set "BUILD_TOOL=%ProgramFiles(x86)%\Microsoft Visual Studio\%%v\%%e\MSBuild\Current\Bin\MSBuild.exe"
            set "IS_MSBUILD=1"
            echo     Found MSBuild: !BUILD_TOOL!
            goto :CHECK_PROJECT
        )
    )
)

echo.
echo [ERROR] Neither dotnet CLI nor MSBuild was found on this system!
echo To compile the application, please install:
echo 1) .NET SDK from https://dotnet.microsoft.com/download OR
echo 2) Visual Studio 2022 with .NET desktop build tools.
echo.
pause
exit /b 1

:CHECK_PROJECT
if not exist "%PROJ_PATH%" (
    echo.
    echo [ERROR] Project file not found:
    echo %PROJ_PATH%
    echo.
    pause
    exit /b 1
)

echo.
echo [2/3] Building application for .NET Framework 4.8 (Release)...
echo.

if not exist "%PUBLISH_DIR%" mkdir "%PUBLISH_DIR%"

if "!IS_MSBUILD!"=="1" (
    "%BUILD_TOOL%" "%PROJ_PATH%" /t:Restore /t:Build /p:Configuration=Release /p:OutputPath="%PUBLISH_DIR%"
) else (
    "%BUILD_TOOL%" build "%PROJ_PATH%" -c Release -o "%PUBLISH_DIR%"
)

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ===============================================================================
    echo [ERROR] Build failed! Please review the error messages above.
    echo ===============================================================================
    echo.
    pause
    exit /b 1
)

echo.
echo [3/3] Preparing output files and configuration...
xcopy /y /q "%PUBLISH_DIR%\*.*" "%ROOT%\" >nul 2>nul
if exist "%SETTINGS_SRC%" (
    copy /y "%SETTINGS_SRC%" "%SETTINGS_DEST%" >nul
    copy /y "%SETTINGS_SRC%" "%PUBLISH_DIR%\appsettings.json" >nul
)

echo.
echo ===============================================================================
echo   SUCCESS! Application build completed (.NET Framework 4.8)!
echo   Target File: %EXE_PATH%
echo ===============================================================================
echo.

:RUN_APP
echo Starting DetelinaPivotReports (.NET Framework 4.8)...
start "" "%EXE_PATH%"
echo.
echo Application started successfully!
timeout /t 3 >nul
exit /b 0
