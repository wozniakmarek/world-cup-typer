param(
    [ValidateSet("start", "stop", "build", "rebuild", "status", "help")]
    [string]$Command = "help",

    [switch]$NoBrowser
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$BackendDir = Join-Path $Root "backend"
$FrontendDir = Join-Path $Root "frontend"
$LocalDir = Join-Path $Root ".local"
$LogDir = Join-Path $LocalDir "logs"

function Write-Step {
    param([string]$Message)
    Write-Host ""
    Write-Host "==> $Message"
}

function Write-Usage {
    Write-Host "World Cup Typer local launcher"
    Write-Host ""
    Write-Host "Usage:"
    Write-Host "  .\typer.ps1 start    Start Postgres, API and frontend dev server"
    Write-Host "  .\typer.ps1 stop     Stop project API/frontend processes and Postgres"
    Write-Host "  .\typer.ps1 build    Restore, test and build backend/frontend"
    Write-Host "  .\typer.ps1 rebuild  Stop, build and start the local stack"
    Write-Host "  .\typer.ps1 status   Show local service status"
    Write-Host ""
    Write-Host "Useful URLs:"
    Write-Host "  Frontend: http://localhost:5173"
    Write-Host "  API:      http://localhost:5000"
    Write-Host "  Swagger:  http://localhost:5000/swagger"
}

function Assert-Command {
    param([string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found in PATH."
    }
}

function Invoke-Checked {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [string]$WorkingDirectory
    )

    Push-Location $WorkingDirectory
    try {
        & $FilePath @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "'$FilePath $($Arguments -join ' ')' failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

function Get-ListeningProcess {
    param([int]$Port)

    $connection = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
        Select-Object -First 1

    if (-not $connection) {
        return $null
    }

    Get-CimInstance Win32_Process -Filter "ProcessId = $($connection.OwningProcess)" -ErrorAction SilentlyContinue
}

function Test-PortOpen {
    param([int]$Port)
    return [bool](Get-ListeningProcess -Port $Port)
}

function Wait-Http {
    param(
        [string]$Url,
        [int]$TimeoutSeconds = 45
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    do {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                return
            }
        }
        catch {
            Start-Sleep -Milliseconds 750
        }
    } while ((Get-Date) -lt $deadline)

    throw "Timed out waiting for $Url."
}

function Stop-ProjectProcessOnPort {
    param([int]$Port)

    $process = Get-ListeningProcess -Port $Port
    if (-not $process) {
        Write-Host "Port ${Port}: nothing to stop."
        return
    }

    $commandLine = [string]$process.CommandLine
    if ($commandLine.IndexOf($Root, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Write-Warning "Port $Port is used by PID $($process.ProcessId), but it does not look like this project. Leaving it running."
        return
    }

    Write-Host "Stopping PID $($process.ProcessId) on port $Port."
    Stop-Process -Id $process.ProcessId -Force
}

function Show-Status {
    Write-Step "Local service status"

    foreach ($port in @(5000, 5173, 4173)) {
        $process = Get-ListeningProcess -Port $port
        if ($process) {
            Write-Host "Port ${port}: LISTEN (PID $($process.ProcessId))"
        }
        else {
            Write-Host "Port ${port}: closed"
        }
    }

    try {
        $health = Invoke-RestMethod -Uri "http://localhost:5000/health/live" -TimeoutSec 3
        Write-Host "API live health: $($health.status)"
    }
    catch {
        Write-Host "API live health: unavailable"
    }

    try {
        $dbHealth = Invoke-RestMethod -Uri "http://localhost:5000/health" -TimeoutSec 3
        Write-Host "API readiness: $($dbHealth.status)"
    }
    catch {
        Write-Host "API readiness: unavailable"
    }
}

function Start-Stack {
    Assert-Command "docker"
    Assert-Command "dotnet"
    Assert-Command "npm.cmd"

    New-Item -ItemType Directory -Force -Path $LogDir | Out-Null

    Write-Step "Starting PostgreSQL"
    Invoke-Checked -FilePath "docker" -Arguments @("compose", "up", "-d") -WorkingDirectory $Root

    if (Test-PortOpen -Port 5000) {
        Write-Host "API already listens on http://localhost:5000."
    }
    else {
        Write-Step "Starting API"
        Start-Process `
            -FilePath "dotnet.exe" `
            -ArgumentList @("run", "--project", "WorldCupTyper.Api", "--launch-profile", "http") `
            -WorkingDirectory $BackendDir `
            -WindowStyle Hidden `
            -RedirectStandardOutput (Join-Path $LogDir "api.log") `
            -RedirectStandardError (Join-Path $LogDir "api.err.log") | Out-Null
    }

    if (Test-PortOpen -Port 5173) {
        Write-Host "Frontend already listens on http://localhost:5173."
    }
    else {
        Write-Step "Starting frontend"
        Start-Process `
            -FilePath "npm.cmd" `
            -ArgumentList @("run", "dev", "--", "--host", "127.0.0.1", "--port", "5173") `
            -WorkingDirectory $FrontendDir `
            -WindowStyle Hidden `
            -RedirectStandardOutput (Join-Path $LogDir "frontend.log") `
            -RedirectStandardError (Join-Path $LogDir "frontend.err.log") | Out-Null
    }

    Write-Step "Waiting for services"
    Wait-Http -Url "http://localhost:5000/health/live"
    Wait-Http -Url "http://localhost:5173"

    Write-Host ""
    Write-Host "Local app is ready:"
    Write-Host "  Frontend: http://localhost:5173"
    Write-Host "  API:      http://localhost:5000"
    Write-Host "  Swagger:  http://localhost:5000/swagger"
    Write-Host ""
    Write-Host "Seed users:"
    Write-Host "  admin@marekwozniak.me / ChangeMe123!"
    Write-Host "  marek@typer.local     / ChangeMe123!"

    if (-not $NoBrowser) {
        Start-Process "http://localhost:5173"
    }
}

function Stop-Stack {
    Write-Step "Stopping frontend/API"
    Stop-ProjectProcessOnPort -Port 5173
    Stop-ProjectProcessOnPort -Port 4173
    Stop-ProjectProcessOnPort -Port 5000

    Write-Step "Stopping PostgreSQL"
    Invoke-Checked -FilePath "docker" -Arguments @("compose", "stop") -WorkingDirectory $Root
}

function Build-Stack {
    Assert-Command "dotnet"
    Assert-Command "npm.cmd"

    Write-Step "Stopping API before build"
    Stop-ProjectProcessOnPort -Port 5000

    Write-Step "Restoring backend"
    Invoke-Checked -FilePath "dotnet" -Arguments @("restore") -WorkingDirectory $BackendDir

    Write-Step "Building backend"
    Invoke-Checked -FilePath "dotnet" -Arguments @("build", "--no-restore") -WorkingDirectory $BackendDir

    Write-Step "Testing backend"
    Invoke-Checked -FilePath "dotnet" -Arguments @("test", "--no-build") -WorkingDirectory $BackendDir

    Write-Step "Installing frontend dependencies"
    Invoke-Checked -FilePath "npm.cmd" -Arguments @("install") -WorkingDirectory $FrontendDir

    Write-Step "Building frontend"
    Invoke-Checked -FilePath "npm.cmd" -Arguments @("run", "build") -WorkingDirectory $FrontendDir
}

switch ($Command) {
    "start" {
        Start-Stack
    }
    "stop" {
        Stop-Stack
    }
    "build" {
        Build-Stack
    }
    "rebuild" {
        Stop-Stack
        Build-Stack
        Start-Stack
    }
    "status" {
        Show-Status
    }
    "help" {
        Write-Usage
    }
}
