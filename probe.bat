@echo off
rem Diagnostica: non cambia le luci
cd /d "%~dp0"
dotnet bin\LegionChromaFlow.dll probe
echo.
pause
