@echo off
rem Quick test: every key red, green, blue (2.5 s each), then restores the profile
cd /d "%~dp0"
dotnet bin\LegionChromaFlow.dll test
echo.
pause
