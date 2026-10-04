' Apre il pannello di controllo (o lo porta in primo piano se e' gia' avviato)
Set fso = CreateObject("Scripting.FileSystemObject")
dir = fso.GetParentFolderName(WScript.ScriptFullName)
Set sh = CreateObject("WScript.Shell")
sh.CurrentDirectory = dir
sh.Run "dotnet """ & dir & "\bin\LegionChromaFlow.dll"" gui", 0, False
