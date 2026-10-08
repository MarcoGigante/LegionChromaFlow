@echo off
schtasks /Delete /TN "LegionChromaFlow" /F
del "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup\LegionChromaFlow.lnk" 2>nul
echo Autostart removed.
pause
