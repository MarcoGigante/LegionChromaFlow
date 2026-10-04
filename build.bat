@echo off
rem Ricompila il programma (serve .NET SDK 8 o successivo: winget install Microsoft.DotNet.SDK.8)
cd /d "%~dp0"
dotnet build src\LegionChromaFlow.csproj -c Release -o bin
if errorlevel 1 (echo. & echo Compilazione FALLITA & pause & exit /b 1)
echo.
echo Compilazione completata.
pause
