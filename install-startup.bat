@echo off
rem OPTIONAL: starts the panel in the tray right after every Windows sign-in.
rem Uses a logon scheduled task (no admin rights needed), which starts sooner than the Startup folder.
cd /d "%~dp0"
set "XML=%TEMP%\lcf-task.xml"
powershell -NoProfile -Command "$u=$env:USERDOMAIN+'\'+$env:USERNAME; $d='%~dp0'.TrimEnd('\'); $x='<?xml version=\"1.0\" encoding=\"UTF-16\"?><Task version=\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\"><Triggers><LogonTrigger><Enabled>true</Enabled><UserId>'+$u+'</UserId></LogonTrigger></Triggers><Principals><Principal id=\"Author\"><UserId>'+$u+'</UserId><LogonType>InteractiveToken</LogonType><RunLevel>LeastPrivilege</RunLevel></Principal></Principals><Settings><MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy><DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries><StopIfGoingOnBatteries>false</StopIfGoingOnBatteries><ExecutionTimeLimit>PT0S</ExecutionTimeLimit><StartWhenAvailable>true</StartWhenAvailable></Settings><Actions Context=\"Author\"><Exec><Command>wscript.exe</Command><Arguments>\"'+$d+'\run-hidden.vbs\"</Arguments><WorkingDirectory>'+$d+'</WorkingDirectory></Exec></Actions></Task>'; Set-Content -Path '%XML%' -Value $x -Encoding Unicode"
schtasks /Create /TN "LegionChromaFlow" /XML "%XML%" /F
del "%XML%" 2>nul
pause
