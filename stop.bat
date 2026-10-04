@echo off
rem Stops the background instance and restores the lighting profile
cd /d "%~dp0"
dotnet bin\LegionChromaFlow.dll stop
