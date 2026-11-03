@echo off
rem Double-click to build this plugin and publish its version as a GitHub release (if new).
rem Pass --build-only (or run "Release.bat --build-only") to just build the package.
set ARGS=
if /i "%~1"=="--build-only" set ARGS=-BuildOnly
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0release.ps1" %ARGS%
echo.
pause
