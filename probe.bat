@echo off
rem Diagnostics: does not change the lights
cd /d "%~dp0"
dotnet bin\LegionChromaFlow.dll probe
echo.
pause
