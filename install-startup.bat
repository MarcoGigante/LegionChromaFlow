@echo off
rem OPTIONAL: starts the panel in the tray at every Windows sign-in (shortcut in the Startup folder)
cd /d "%~dp0"
powershell -NoProfile -Command "$s=(New-Object -ComObject WScript.Shell).CreateShortcut($env:APPDATA+'\Microsoft\Windows\Start Menu\Programs\Startup\LegionChromaFlow.lnk'); $s.TargetPath='wscript.exe'; $s.Arguments='\"%~dp0run-hidden.vbs\"'; $s.WorkingDirectory='%~dp0'; $s.Save()"
echo Shortcut created in the Startup folder.
pause
