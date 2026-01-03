param(
    [string]$ServiceName = "OcrWorkerPaddle",
    [string]$Port = "50051",
    [string]$WorkingDirectory = "C:\\ocr-system\\src\\Ocr.Worker.Paddle",
    [string]$PythonPath = "python.exe"
)

$ErrorActionPreference = "Stop"

$env:WORKER_PORT = $Port
$env:WORKER_LANG = if ([string]::IsNullOrWhiteSpace($env:WORKER_LANG)) { "es" } else { $env:WORKER_LANG }

$nssm = "nssm.exe"
$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Definition
$localNssm = Join-Path $scriptDirectory $nssm

if (-not (Get-Command $nssm -ErrorAction SilentlyContinue)) {
    if (Test-Path $localNssm) {
        $nssm = $localNssm
    }
    else {
        throw "nssm.exe is required to install the worker as a service"
    }
}

$scriptPath = Join-Path $WorkingDirectory "server.py"

& $nssm install $ServiceName $PythonPath "$scriptPath" | Out-Null
& $nssm set $ServiceName AppDirectory $WorkingDirectory | Out-Null
& $nssm set $ServiceName AppEnvironmentExtra @("WORKER_PORT=$Port", "WORKER_LANG=$($env:WORKER_LANG)") | Out-Null

Write-Host "Service $ServiceName configured on port $Port"
