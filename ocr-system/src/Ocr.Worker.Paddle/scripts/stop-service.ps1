param(
    [string]$ServiceName = "OcrWorkerPaddle"
)

$ErrorActionPreference = "Stop"

function Test-IsAdministrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-NssmPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScriptDirectory
    )

    $localNssm = Join-Path $ScriptDirectory "nssm.exe"
    if (Test-Path $localNssm) {
        return $localNssm
    }

    $command = Get-Command "nssm.exe" -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    throw "nssm.exe is required to stop the service. Place it next to this script or add it to PATH."
}

if (-not (Test-IsAdministrator)) {
    throw "Administrator access is required to stop the service. Re-run PowerShell como Administrador."
}

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Definition
$nssm = Get-NssmPath -ScriptDirectory $scriptDirectory

& $nssm stop $ServiceName | Out-Null

Write-Host "Service $ServiceName detenido."
