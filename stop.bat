@echo off
rem Ferma l'istanza in background e ripristina le luci del profilo
cd /d "%~dp0"
dotnet bin\LegionChromaFlow.dll stop
