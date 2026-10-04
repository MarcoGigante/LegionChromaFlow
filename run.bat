@echo off
rem Starts the effect in a visible console window (Ctrl+C to stop)
cd /d "%~dp0"
dotnet bin\LegionChromaFlow.dll run
echo.
pause
