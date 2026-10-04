@echo off
rem Avvia l'effetto con la finestra visibile (Ctrl+C per fermare)
cd /d "%~dp0"
dotnet bin\LegionChromaFlow.dll run
echo.
pause
