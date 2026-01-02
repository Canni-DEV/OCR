#!/usr/bin/env bash
set -euo pipefail
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROTO_PATH="$ROOT_DIR/proto/ocrworker.proto"
PY_OUT="$ROOT_DIR/src/Ocr.Worker.Paddle"
DOTNET_PROJECT="$ROOT_DIR/src/Ocr.Api/Ocr.Api.csproj"

python -m grpc_tools.protoc -I"$ROOT_DIR/proto" --python_out="$PY_OUT" --grpc_python_out="$PY_OUT" "$PROTO_PATH"

dotnet clean "$DOTNET_PROJECT" >/dev/null || true
dotnet build "$DOTNET_PROJECT" >/dev/null || true
