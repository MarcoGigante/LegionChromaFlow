' Starts the panel + effect silently in the tray. To stop it: stop.bat, or Exit in the tray menu
Set fso = CreateObject("Scripting.FileSystemObject")
dir = fso.GetParentFolderName(WScript.ScriptFullName)
Set sh = CreateObject("WScript.Shell")
sh.CurrentDirectory = dir
sh.Run "dotnet """ & dir & "\bin\LegionChromaFlow.dll"" gui tray", 0, False
