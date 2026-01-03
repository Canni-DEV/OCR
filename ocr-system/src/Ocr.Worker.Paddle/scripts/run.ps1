param(
    [string]$ServiceName = "OcrWorkerPaddle",
    [string]$Port = "50051",
    [string]$WorkingDirectory,
    [string]$PythonPath,
    [string]$VenvPath,
    [string]$VenvName = ".venv",
    [string]$Lang = "es",
    [int]$Concurrency = 1,
    [switch]$DisableAngleCls
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

function Resolve-WorkingDirectory {
    param(
        [string]$WorkingDirectory,
        [string]$ScriptDirectory
    )

    if ($WorkingDirectory) {
        return (Resolve-Path $WorkingDirectory).Path
    }

    $workerRoot = Split-Path $ScriptDirectory -Parent
    return (Resolve-Path $workerRoot).Path
}

function Resolve-PythonPath {
    param(
        [string]$PythonPath,
        [string]$WorkingDirectory,
        [string]$VenvPath,
        [string]$VenvName
    )

    if ($PythonPath) {
        return $PythonPath
    }

    $candidateVenv = $VenvPath
    if (-not $candidateVenv) {
        $candidateVenv = Join-Path $WorkingDirectory $VenvName
    }

    $venvPython = Join-Path $candidateVenv "Scripts/python.exe"
    if (Test-Path $venvPython) {
        return $venvPython
    }

    return "python.exe"
}

if (-not (Test-IsAdministrator)) {
    throw "Administrator access is required to install the service. Re-run PowerShell como Administrador."
}

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Definition
$resolvedWorkingDirectory = Resolve-WorkingDirectory -WorkingDirectory $WorkingDirectory -ScriptDirectory $scriptDirectory

if (-not (Test-Path $resolvedWorkingDirectory)) {
    throw "Working directory '$resolvedWorkingDirectory' does not exist."
}

$scriptPath = Join-Path $resolvedWorkingDirectory "server.py"
if (-not (Test-Path $scriptPath)) {
    throw "Could not find server.py at '$scriptPath'. Verify WorkingDirectory."
}

$resolvedPython = Resolve-PythonPath -PythonPath $PythonPath -WorkingDirectory $resolvedWorkingDirectory -VenvPath $VenvPath -VenvName $VenvName

$env:WORKER_PORT = $Port
$env:WORKER_LANG = if ([string]::IsNullOrWhiteSpace($Lang)) { "es" } else { $Lang }
$envVariables = @("WORKER_PORT=$Port", "WORKER_LANG=$($env:WORKER_LANG)", "WORKER_CONCURRENCY=$Concurrency")

$useAngleCls = -not $DisableAngleCls
$envVariables += "PADDLE_USE_ANGLE_CLS=$($useAngleCls.ToString().ToLower())"

$nssm = Get-NssmPath -ScriptDirectory $scriptDirectory

Write-Host "Installing service '$ServiceName' using Python at '$resolvedPython'..."
& $nssm install $ServiceName $resolvedPython "$scriptPath" | Out-Null
& $nssm set $ServiceName AppDirectory $resolvedWorkingDirectory | Out-Null
& $nssm set $ServiceName AppEnvironmentExtra $envVariables | Out-Null

Write-Host "Service $ServiceName configured on port $Port (lang=$($env:WORKER_LANG), angle_cls=$useAngleCls, concurrency=$Concurrency)"
