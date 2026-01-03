param(
    [string]$PythonPath = "python"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$workerPath = Join-Path $root "src/Ocr.Worker.Paddle"
$proto = Join-Path $root "proto/ocrworker.proto"
$pyOut = Join-Path $root "src/Ocr.Worker.Paddle"
$project = Join-Path $root "src/Ocr.Api/Ocr.Api.csproj"

$protoVenvPath = Join-Path $workerPath ".venv-protos"
$protoRequirements = Join-Path $workerPath "requirements.protos.txt"

# grpcio-tools 1.48.x (needed to stay compatible with protobuf 3.20.x) ships
# prebuilt wheels only up to Python 3.10. Fail fast with a clear hint if the
# supplied Python is too new.
$pythonVersion = & $PythonPath -c "import sys; print(f'{sys.version_info.major}.{sys.version_info.minor}')"

if ([version]$pythonVersion -gt [version]"3.10") {
    throw "grpcio-tools 1.48.x only provides wheels up to Python 3.10. Run this script with Python 3.10 (use -PythonPath) to regenerate stubs."
}

if (-not (Test-Path $protoRequirements)) {
    throw "Could not find proto requirements file at '$protoRequirements'"
}

if (-not (Test-Path $protoVenvPath)) {
    Write-Host "Creating proto virtual environment in $protoVenvPath ..."
    & python -m venv $protoVenvPath
}

$protoPython = Join-Path $protoVenvPath "Scripts/python.exe"
if (-not (Test-Path $protoPython)) {
    throw "Python executable not found in proto virtual environment: $protoPython"
}

Write-Host "Ensuring proto tools dependencies are installed..."
& $protoPython -m pip install --upgrade pip
& $protoPython -m pip install -r $protoRequirements

& $protoPython -m grpc_tools.protoc -I"$($root)/proto" --python_out="$pyOut" --grpc_python_out="$pyOut" "$proto"

dotnet build "$project"
