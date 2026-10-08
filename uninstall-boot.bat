@echo off
rem Removes the boot-time lighting task (asks for administrator rights once).
net session >nul 2>&1
if %errorlevel% neq 0 (
  echo Requesting administrator rights...
  powershell -NoProfile -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
  exit /b
)
schtasks /Delete /TN "LegionChromaFlow Boot" /F
echo Boot lighting removed.
pause
