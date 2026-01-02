param(
    [string]$ServiceName = "OcrWorkerPaddle",
    [string]$Port = "50051",
    [string]$WorkingDirectory = "C:\\ocr-system\\src\\Ocr.Worker.Paddle",
    [string]$PythonPath = "python.exe"
)

$ErrorActionPreference = "Stop"

$env:WORKER_PORT = $Port
$env:WORKER_LANG = $env:WORKER_LANG ?? "es"

$nssm = "nssm.exe"
if (-not (Get-Command $nssm -ErrorAction SilentlyContinue)) {
    throw "nssm.exe is required to install the worker as a service"
}

$scriptPath = Join-Path $WorkingDirectory "server.py"

& $nssm install $ServiceName $PythonPath "$scriptPath" | Out-Null
& $nssm set $ServiceName AppDirectory $WorkingDirectory | Out-Null
& $nssm set $ServiceName AppEnvironmentExtra "WORKER_PORT=$Port" | Out-Null

Write-Host "Service $ServiceName configured on port $Port"
