param(
    [string]$WorkerPath,
    [string]$PythonPath = "python.exe",
    [string]$VenvName = ".venv",
    [string]$ProtoVenvName = ".venv-protos",
    [switch]$ForceRecreate,
    [switch]$InstallProtoTools
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

if ($InstallProtoTools) {
    $protoRequirements = Join-Path $WorkerPath "requirements.protos.txt"
    if (-not (Test-Path $protoRequirements)) {
        Write-Warning "Could not find requirements.protos.txt; skipping proto tool installation."
        return
    }

    $protoVenvPath = Join-Path $WorkerPath $ProtoVenvName
    if ($ForceRecreate -and (Test-Path $protoVenvPath)) {
        Remove-Item $protoVenvPath -Recurse -Force
    }

    if (-not (Test-Path $protoVenvPath)) {
        Write-Host "Creating virtual environment for proto tools in $protoVenvPath ..."
        & $PythonPath -m venv $protoVenvPath
    }

    $protoVenvPython = Join-Path $protoVenvPath "Scripts/python.exe"
    if (-not (Test-Path $protoVenvPython)) {
        throw "Python executable not found in virtual environment: $protoVenvPython"
    }

    Write-Host "Upgrading pip in proto tools environment..."
    & $protoVenvPython -m pip install --upgrade pip

    Write-Host "Installing proto-generation dependencies..."
    & $protoVenvPython -m pip install -r $protoRequirements

    Write-Host "Proto tools virtual environment ready at $protoVenvPath"
    Write-Host "Use '$protoVenvPython -m grpc_tools.protoc ...' to regenerate stubs."
}
