param(
    [string]$WorkerPath,
    [string]$PythonPath = "python.exe",
    [string]$VenvName = ".venv",
    [switch]$ForceRecreate
)

$ErrorActionPreference = "Stop"

# Resolve worker directory relative to repo root if not provided.
if (-not $WorkerPath) {
    $root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
    $WorkerPath = Join-Path $root "src/Ocr.Worker.Paddle"
}

if (-not (Test-Path $WorkerPath)) {
    throw "Worker path '$WorkerPath' does not exist"
}

$WorkerPath = (Resolve-Path $WorkerPath).Path
$requirements = Join-Path $WorkerPath "requirements.txt"
if (-not (Test-Path $requirements)) {
    throw "Could not find requirements.txt under '$WorkerPath'"
}

$venvPath = Join-Path $WorkerPath $VenvName

if ($ForceRecreate -and (Test-Path $venvPath)) {
    Remove-Item $venvPath -Recurse -Force
}

if (-not (Test-Path $venvPath)) {
    Write-Host "Creating virtual environment in $venvPath ..."
    & $PythonPath -m venv $venvPath
}

$venvPython = Join-Path $venvPath "Scripts/python.exe"
if (-not (Test-Path $venvPython)) {
    throw "Python executable not found in virtual environment: $venvPython"
}

Write-Host "Upgrading pip..."
& $venvPython -m pip install --upgrade pip

Write-Host "Installing dependencies from requirements.txt..."
& $venvPython -m pip install -r $requirements

Write-Host "Virtual environment ready at $venvPath"
Write-Host "Use '$venvPython server.py' to start the worker."
