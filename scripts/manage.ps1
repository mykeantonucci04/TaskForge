<#
.SYNOPSIS
    Management script for TaskForge enterprise backend solution.
.DESCRIPTION
    Provides shortcuts for build, test, docker, database migration, and run operations.
#>

param(
    [Parameter(Mandatory=$true, Position=0)]
    [ValidateSet('build', 'test', 'run', 'docker-up', 'docker-down', 'ef-migration', 'ef-update', 'clean')]
    [string]$Action,

    [Parameter(Mandatory=$false, Position=1)]
    [string]$MigrationName
)

$ErrorActionPreference = 'Stop'
$RootDir = Split-Path -Parent $PSScriptRoot

Set-Location $RootDir

switch ($Action) {
    'build' {
        Write-Host "Building TaskForge Solution..." -ForegroundColor Cyan
        dotnet build TaskForge.sln
    }
    'test' {
        Write-Host "Running TaskForge Tests..." -ForegroundColor Cyan
        dotnet test TaskForge.sln
    }
    'run' {
        Write-Host "Running TaskForge Api..." -ForegroundColor Cyan
        dotnet run --project src/TaskForge.Api/TaskForge.Api.csproj
    }
    'docker-up' {
        Write-Host "Starting Docker Compose services..." -ForegroundColor Cyan
        docker-compose -f docker/docker-compose.yml up -d
    }
    'docker-down' {
        Write-Host "Stopping Docker Compose services..." -ForegroundColor Cyan
        docker-compose -f docker/docker-compose.yml down
    }
    'ef-migration' {
        if (-not $MigrationName) {
            $MigrationName = Read-Host "Enter migration name"
        }
        Write-Host "Adding EF Core Migration '$MigrationName'..." -ForegroundColor Cyan
        dotnet ef migrations add $MigrationName --project src/TaskForge.Infrastructure --startup-project src/TaskForge.Api --output-dir Persistence/Migrations
    }
    'ef-update' {
        Write-Host "Updating database..." -ForegroundColor Cyan
        dotnet ef database update --project src/TaskForge.Infrastructure --startup-project src/TaskForge.Api
    }
    'clean' {
        Write-Host "Cleaning TaskForge Solution..." -ForegroundColor Cyan
        dotnet clean TaskForge.sln
    }
}
