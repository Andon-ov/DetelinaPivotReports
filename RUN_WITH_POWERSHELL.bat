@echo off
title Detelina Pivot Reports - PowerShell Launcher
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Press any key to close this window...
    pause >nul
)
