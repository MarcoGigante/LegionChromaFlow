# Installs a startup task that lights the keyboard as soon as Windows boots (before anyone signs in).
# Run it through install-boot.bat (it needs administrator rights). Remove it with uninstall-boot.bat.
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$dll = Join-Path $root 'bin\LegionChromaFlow.dll'
if (-not (Test-Path $dll)) { throw "bin\LegionChromaFlow.dll not found. Run build.bat first." }
$dotnet = (Get-Command dotnet -ErrorAction Stop).Source

$cmd = [Security.SecurityElement]::Escape($dotnet)
$argText = [Security.SecurityElement]::Escape("`"$dll`" boot")
$dir = [Security.SecurityElement]::Escape($root)

$xml = @"
<?xml version="1.0" encoding="UTF-16"?>
<Task version="1.2" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
  <RegistrationInfo><Description>LegionChromaFlow: keyboard lighting from Windows start, before sign-in</Description></RegistrationInfo>
  <Triggers><BootTrigger><Enabled>true</Enabled></BootTrigger></Triggers>
  <Principals><Principal id="Author"><UserId>S-1-5-18</UserId><RunLevel>HighestAvailable</RunLevel></Principal></Principals>
  <Settings>
    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
    <StartWhenAvailable>true</StartWhenAvailable>
    <RestartOnFailure><Interval>PT1M</Interval><Count>3</Count></RestartOnFailure>
  </Settings>
  <Actions Context="Author"><Exec><Command>$cmd</Command><Arguments>$argText</Arguments><WorkingDirectory>$dir</WorkingDirectory></Exec></Actions>
</Task>
"@

$tmp = Join-Path $env:TEMP 'lcf-boot.xml'
Set-Content -Path $tmp -Value $xml -Encoding Unicode
schtasks /Create /TN "LegionChromaFlow Boot" /XML $tmp /F
[IO.File]::Delete($tmp)
Write-Host ""
Write-Host "Done. From the next start the keyboard lights up before sign-in; the panel takes over when you sign in."
Write-Host "Tip: open the panel once so the current wallpaper is cached for the boot lighting."
