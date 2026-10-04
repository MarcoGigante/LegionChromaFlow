' Avvia pannello + effetto in background (icona nell'area di notifica). Per fermarlo: stop.bat o Esci dal menu dell'icona
Set fso = CreateObject("Scripting.FileSystemObject")
dir = fso.GetParentFolderName(WScript.ScriptFullName)
Set sh = CreateObject("WScript.Shell")
sh.CurrentDirectory = dir
sh.Run "dotnet """ & dir & "\bin\LegionChromaFlow.dll"" gui tray", 0, False
