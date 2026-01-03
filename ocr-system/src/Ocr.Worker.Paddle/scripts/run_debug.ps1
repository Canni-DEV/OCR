param(
    [string]$WorkerPath,
    [string]$PythonPath = "python.exe",
    [string]$VenvName = ".venv",
    [switch]$ForceRecreate,
    [int]$Port = 50051,
    [string]$Lang = "es",
    [int]$Concurrency = 1,
    [switch]$DisableAngleCls
)

$ErrorActionPreference = "Stop"

function Resolve-WorkerPath {
    param(
        [string]$WorkerPath,
        [string]$ScriptDirectory
    )

    if ($WorkerPath) {
        return (Resolve-Path $WorkerPath).Path
    }

    $defaultPath = Split-Path $ScriptDirectory -Parent
    return (Resolve-Path $defaultPath).Path
}

function Ensure-Venv {
    param(
        [string]$VenvPath,
        [string]$PythonPath,
        [switch]$ForceRecreate
    )

    if ($ForceRecreate -and (Test-Path $VenvPath)) {
        Remove-Item $VenvPath -Recurse -Force
    }

    if (-not (Test-Path $VenvPath)) {
        Write-Host "Creating virtual environment in $VenvPath ..."
        & $PythonPath -m venv $VenvPath
    }
}

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Definition
$resolvedWorkerPath = Resolve-WorkerPath -WorkerPath $WorkerPath -ScriptDirectory $scriptDirectory
$requirements = Join-Path $resolvedWorkerPath "requirements.txt"
$serverPath = Join-Path $resolvedWorkerPath "server.py"

if (-not (Test-Path $requirements)) {
    throw "Could not find requirements.txt under '$resolvedWorkerPath'."
}

if (-not (Test-Path $serverPath)) {
    throw "Could not find server.py under '$resolvedWorkerPath'."
}

$venvPath = Join-Path $resolvedWorkerPath $VenvName
Ensure-Venv -VenvPath $venvPath -PythonPath $PythonPath -ForceRecreate:$ForceRecreate

$venvPython = Join-Path $venvPath "Scripts/python.exe"
if (-not (Test-Path $venvPython)) {
    throw "Python executable not found in virtual environment: $venvPython"
}

Write-Host "Upgrading pip in $venvPath ..."
& $venvPython -m pip install --upgrade pip

Write-Host "Installing dependencies from $requirements ..."
& $venvPython -m pip install -r $requirements

$env:WORKER_PORT = $Port
$env:WORKER_LANG = if ([string]::IsNullOrWhiteSpace($Lang)) { "es" } else { $Lang }
$env:WORKER_CONCURRENCY = $Concurrency
$env:PADDLE_USE_ANGLE_CLS = (-not $DisableAngleCls).ToString().ToLower()

Write-Host "Starting worker at $serverPath on port $Port (lang=$($env:WORKER_LANG), angle_cls=$env:PADDLE_USE_ANGLE_CLS, concurrency=$Concurrency)..."

Push-Location $resolvedWorkerPath
try {
    & $venvPython $serverPath
} finally {
    Pop-Location
}
