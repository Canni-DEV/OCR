param(
    [string]$ServiceName = "OcrWorkerPaddle",
    [string]$Port = "50051",
    [string]$WorkingDirectory = "C:\\ocr-system\\src\\Ocr.Worker.Paddle",
    [string]$PythonPath = "python.exe"
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

    throw "nssm.exe is required to install the worker as a service. Place it next to this script or add it to PATH."
}

if (-not (Test-IsAdministrator)) {
    throw "Administrator access is required to install the service. Re-run PowerShell como Administrador."
}

$env:WORKER_PORT = $Port
if ([string]::IsNullOrWhiteSpace($env:WORKER_LANG)) {
    $env:WORKER_LANG = "es"
}

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Definition
$nssm = Get-NssmPath -ScriptDirectory $scriptDirectory
$scriptPath = Join-Path $WorkingDirectory "server.py"
$envVariables = @("WORKER_PORT=$Port", "WORKER_LANG=$($env:WORKER_LANG)")

& $nssm install $ServiceName $PythonPath "$scriptPath" | Out-Null
& $nssm set $ServiceName AppDirectory $WorkingDirectory | Out-Null
& $nssm set $ServiceName AppEnvironmentExtra $envVariables | Out-Null

Write-Host "Service $ServiceName configured on port $Port"
