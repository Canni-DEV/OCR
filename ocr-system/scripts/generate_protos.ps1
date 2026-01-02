$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$proto = Join-Path $root "proto/ocrworker.proto"
$pyOut = Join-Path $root "src/Ocr.Worker.Paddle"
$project = Join-Path $root "src/Ocr.Api/Ocr.Api.csproj"

python -m grpc_tools.protoc -I"$($root)/proto" --python_out="$pyOut" --grpc_python_out="$pyOut" "$proto"

dotnet build "$project"
