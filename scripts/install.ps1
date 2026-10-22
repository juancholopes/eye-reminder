# Instala Eye Reminder para el usuario actual (sin permisos de administrador):
# publica en Release, copia a %LOCALAPPDATA%\Programs\EyeReminder y crea el acceso
# directo en el menú Inicio. Con -Startup también arranca al iniciar sesión.
param(
    [switch]$Startup,
    [switch]$NoLaunch
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\EyeReminder\EyeReminder.csproj'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\EyeReminder'
$exePath = Join-Path $installDir 'EyeReminder.exe'

Get-Process EyeReminder -ErrorAction SilentlyContinue | Stop-Process
Start-Sleep -Milliseconds 500

# Se usa la salida de build y no "dotnet publish": en apps WinUI sin empaquetar, publish
# omite EyeReminder.pri y los .xbf de las vistas y la app se cierra al arrancar.
Write-Host 'Compilando en Release ...'
$buildDir = Join-Path $PSScriptRoot '..\EyeReminder\bin\install'
dotnet build $project -c Release -p:Platform=x64 -r win-x64 -o $buildDir -nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'dotnet build falló.' }

Write-Host "Copiando a $installDir ..."
if (Test-Path $installDir) { Remove-Item $installDir -Recurse -Force }
Copy-Item $buildDir $installDir -Recurse

function New-Shortcut([string]$path) {
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($path)
    $shortcut.TargetPath = $exePath
    $shortcut.WorkingDirectory = $installDir
    $shortcut.IconLocation = "$exePath,0"
    $shortcut.Description = 'Recordatorio 20-20-20 para descansar la vista'
    $shortcut.Save()
}

$programs = [Environment]::GetFolderPath('Programs')
New-Shortcut (Join-Path $programs 'Eye Reminder.lnk')
Write-Host 'Acceso directo creado en el menú Inicio.'

if ($Startup) {
    New-Shortcut (Join-Path ([Environment]::GetFolderPath('Startup')) 'Eye Reminder.lnk')
    Write-Host 'La app arrancará al iniciar sesión.'
}

if (-not $NoLaunch) {
    Start-Process $exePath -WorkingDirectory $installDir
    Write-Host 'Eye Reminder está abierto.'
}
