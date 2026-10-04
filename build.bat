@echo off
rem Rebuilds the program (needs the .NET SDK 8 or later: winget install Microsoft.DotNet.SDK.8)
cd /d "%~dp0"
dotnet build src\LegionChromaFlow.csproj -c Release -o bin
if errorlevel 1 (echo. & echo BUILD FAILED & pause & exit /b 1)
echo.
echo Build completed.
pause
