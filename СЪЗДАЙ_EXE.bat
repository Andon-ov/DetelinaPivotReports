@echo off
setlocal enabledelayedexpansion
title Build DetelinaPivotReports.exe (.NET Framework 4.8)

echo ===============================================================================
echo     Building Detelina / Eltrade Pivot Reports (.NET Framework 4.8 WPF)
echo ===============================================================================
echo.

set "ROOT=%~dp0"
if "%ROOT:~-1%"=="\" set "ROOT=%ROOT:~0,-1%"

set "PROJ_PATH=%ROOT%\DetelinaPivotReports\DetelinaPivotReports\DetelinaPivotReports.csproj"
set "EXE_PATH=%ROOT%\DetelinaPivotReports.exe"
set "PUBLISH_DIR=%ROOT%\publish"
set "SETTINGS_SRC=%ROOT%\DetelinaPivotReports\DetelinaPivotReports\appsettings.json"
set "SETTINGS_DEST=%ROOT%\appsettings.json"

echo [1/3] Checking for build tools (dotnet / msbuild)...
set "BUILD_TOOL=dotnet"
set "IS_MSBUILD=0"

where dotnet >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    echo     Found in system PATH: dotnet
    goto :CHECK_PROJ
)

if exist "%ProgramFiles%\dotnet\dotnet.exe" (
    set "BUILD_TOOL=%ProgramFiles%\dotnet\dotnet.exe"
    echo     Found: %ProgramFiles%\dotnet\dotnet.exe
    goto :CHECK_PROJ
)

if exist "C:\Program Files\dotnet\dotnet.exe" (
    set "BUILD_TOOL=C:\Program Files\dotnet\dotnet.exe"
    echo     Found: C:\Program Files\dotnet\dotnet.exe
    goto :CHECK_PROJ
)

if exist "%SystemDrive%\Program Files\dotnet\dotnet.exe" (
    set "BUILD_TOOL=%SystemDrive%\Program Files\dotnet\dotnet.exe"
    goto :CHECK_PROJ
)

if exist "%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe" (
    set "BUILD_TOOL=%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe"
    goto :CHECK_PROJ
)

where msbuild >nul 2>nul
if %ERRORLEVEL% EQU 0 (
    set "BUILD_TOOL=msbuild"
    set "IS_MSBUILD=1"
    echo     Found in system PATH: msbuild
    goto :CHECK_PROJ
)

for %%v in (2022 2019) do (
    for %%e in (Enterprise Professional Community BuildTools) do (
        if exist "%ProgramFiles%\Microsoft Visual Studio\%%v\%%e\MSBuild\Current\Bin\MSBuild.exe" (
            set "BUILD_TOOL=%ProgramFiles%\Microsoft Visual Studio\%%v\%%e\MSBuild\Current\Bin\MSBuild.exe"
            set "IS_MSBUILD=1"
            echo     Found MSBuild: !BUILD_TOOL!
            goto :CHECK_PROJ
        )
        if exist "%ProgramFiles(x86)%\Microsoft Visual Studio\%%v\%%e\MSBuild\Current\Bin\MSBuild.exe" (
            set "BUILD_TOOL=%ProgramFiles(x86)%\Microsoft Visual Studio\%%v\%%e\MSBuild\Current\Bin\MSBuild.exe"
            set "IS_MSBUILD=1"
            echo     Found MSBuild: !BUILD_TOOL!
            goto :CHECK_PROJ
        )
    )
)

echo.
echo [ERROR] Neither dotnet CLI nor MSBuild was found on this system!
echo Please install .NET SDK (https://dotnet.microsoft.com/download) or Visual Studio.
echo.
pause
exit /b 1

:CHECK_PROJ
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
echo [3/3] Copying output files to root folder...
xcopy /y /q "%PUBLISH_DIR%\*.*" "%ROOT%\" >nul 2>nul
if exist "%SETTINGS_SRC%" (
    copy /y "%SETTINGS_SRC%" "%SETTINGS_DEST%" >nul
    copy /y "%SETTINGS_SRC%" "%PUBLISH_DIR%\appsettings.json" >nul
)

echo.
echo ===============================================================================
echo   SUCCESS! DetelinaPivotReports.exe (.NET Framework 4.8) is ready!
echo   Location: %EXE_PATH%
echo   This exe runs directly on Windows using pre-installed .NET Framework 4.8.
echo ===============================================================================
echo.

echo Would you like to launch the application now? (Y/N)
set /p START_NOW="Choice [Y/N]: "
if /i "%START_NOW%"=="Y" (
    start "" "%EXE_PATH%"
)

exit /b 0
