$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$workerPath = Join-Path $root "src/Ocr.Worker.Paddle"
$proto = Join-Path $root "proto/ocrworker.proto"
$pyOut = Join-Path $root "src/Ocr.Worker.Paddle"
$project = Join-Path $root "src/Ocr.Api/Ocr.Api.csproj"

$protoVenvPath = Join-Path $workerPath ".venv-protos"
$protoRequirements = Join-Path $workerPath "requirements.protos.txt"

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
