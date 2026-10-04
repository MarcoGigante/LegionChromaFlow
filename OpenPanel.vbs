' Opens the control panel (or brings it to the front if it is already running)
Set fso = CreateObject("Scripting.FileSystemObject")
dir = fso.GetParentFolderName(WScript.ScriptFullName)
Set sh = CreateObject("WScript.Shell")
sh.CurrentDirectory = dir
sh.Run "dotnet """ & dir & "\bin\LegionChromaFlow.dll"" gui", 0, False
