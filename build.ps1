# PowerShell script to build and run DetelinaPivotReports (.NET Framework 4.8 WPF)
$ErrorActionPreference = "Continue"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  Building DetelinaPivotReports (.NET Framework 4.8 WPF)" -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host ""

$root = $PSScriptRoot
$projectPath = Join-Path $root "DetelinaPivotReports\DetelinaPivotReports\DetelinaPivotReports.csproj"
$outPublish = Join-Path $root "publish"
$exePath = Join-Path $root "DetelinaPivotReports.exe"
$appSettingsSource = Join-Path $root "DetelinaPivotReports\DetelinaPivotReports\appsettings.json"

# Find dotnet or msbuild
$dotnetCmd = "dotnet"
if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
    if (Test-Path "$env:ProgramFiles\dotnet\dotnet.exe") {
        $dotnetCmd = "$env:ProgramFiles\dotnet\dotnet.exe"
    } elseif (Test-Path "C:\Program Files\dotnet\dotnet.exe") {
        $dotnetCmd = "C:\Program Files\dotnet\dotnet.exe"
    } else {
        Write-Host "[ERROR] dotnet was not found!" -ForegroundColor Red
        Write-Host "Please ensure .NET SDK or Visual Studio is installed." -ForegroundColor Yellow
        Read-Host "Press Enter to exit..."
        exit 1
    }
}

Write-Host "[1/3] Using build tool: $dotnetCmd" -ForegroundColor Green
Write-Host "[2/3] Building project (Release, .NET Framework 4.8)..." -ForegroundColor Yellow

if (Test-Path $outPublish) {
    Remove-Item $outPublish -Recurse -Force -ErrorAction SilentlyContinue
}

& $dotnetCmd build $projectPath -c Release -o $outPublish

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "[ERROR] Build failed with exit code $LASTEXITCODE!" -ForegroundColor Red
    Read-Host "Press Enter to review errors..."
    exit 1
}

Write-Host "[3/3] Preparing output files and configuration..." -ForegroundColor Yellow

Get-ChildItem -Path $outPublish | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination $root -Force
}

if (Test-Path $appSettingsSource) {
    Copy-Item -Path $appSettingsSource -Destination (Join-Path $outPublish "appsettings.json") -Force
    Copy-Item -Path $appSettingsSource -Destination (Join-Path $root "appsettings.json") -Force
}

Write-Host ""
Write-Host "=================================================================" -ForegroundColor Green
Write-Host " SUCCESS! Application is ready for .NET Framework 4.8:" -ForegroundColor Green
Write-Host "   $exePath" -ForegroundColor White
Write-Host "=================================================================" -ForegroundColor Green
Write-Host ""

$ans = Read-Host "Would you like to launch the application now? (Y/N) [Y]"
if ($ans -eq "" -or $ans.ToUpper() -eq "Y") {
    Start-Process -FilePath $exePath
    Write-Host "Application launched!" -ForegroundColor Cyan
}
