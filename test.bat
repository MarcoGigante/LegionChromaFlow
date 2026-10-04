@echo off
rem Prova rapida: tutti i tasti rosso, verde, blu (2,5 s ciascuno), poi ripristina
cd /d "%~dp0"
dotnet bin\LegionChromaFlow.dll test
echo.
pause
