@echo off
rem OPZIONALE: avvia l'effetto in background ad ogni accesso a Windows (collegamento nella cartella Esecuzione automatica)
cd /d "%~dp0"
powershell -NoProfile -Command "$s=(New-Object -ComObject WScript.Shell).CreateShortcut($env:APPDATA+'\Microsoft\Windows\Start Menu\Programs\Startup\LegionChromaFlow.lnk'); $s.TargetPath='wscript.exe'; $s.Arguments='\"%~dp0run-hidden.vbs\"'; $s.WorkingDirectory='%~dp0'; $s.Save()"
echo Collegamento creato nella cartella Esecuzione automatica.
pause
