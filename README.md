# TaskForge - Enterprise Multi-Tenant Backend (.NET 10)

TaskForge is a high-performance, enterprise-grade multi-tenant project management backend API built with **.NET 10**, **Clean Architecture**, and **PostgreSQL** schema-per-tenant isolation.

## Architecture

The solution follows a strict 4-Layer Clean Architecture + Shared Kernel design:

```
TaskForge.sln (or TaskForge.slnx)
├── src/
│   ├── TaskForge.Domain/              → Entities, Value Objects, Enums, Repo Interfaces, Domain Events (Zero External Dependencies)
│   ├── TaskForge.Application/         → CQRS (MediatR), FluentValidation, Mapperly DTO Mappers, Service Interfaces
│   ├── TaskForge.Infrastructure/      → EF Core 10, Dynamic Schema DbContext, Repositories, S3 Storage, SignalR, Email
│   ├── TaskForge.Api/                 → ASP.NET Core 10 Minimal APIs, JWT Auth, Tenant Middleware, OpenAPI 3.1, Health Checks
│   └── TaskForge.Shared/              → Shared Kernel: Result<T> pattern, Custom Exceptions, Constants
├── tests/
│   ├── TaskForge.Domain.UnitTests/
│   ├── TaskForge.Application.UnitTests/
│   ├── TaskForge.Infrastructure.IntegrationTests/ → Testcontainers for real PostgreSQL
│   └── TaskForge.Api.IntegrationTests/            → WebApplicationFactory + Testcontainers
├── docs/                               → Architecture, API Contract, ADRs
├── docker/                             → Multi-stage Dockerfile (.NET 10) & docker-compose.yml
├── .github/workflows/                  → GitHub Actions CI/CD Pipeline
├── Directory.Build.props               → Centralized Build & Code Analysis Configuration
├── Directory.Packages.props            → Central Package Management (CPM)
├── Makefile / scripts/manage.ps1       → Development CLI commands
```

## Quick Start (Local Development)

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/)

### 1. Run PostgreSQL using Docker Compose
```bash
docker-compose up -d
```
Or via Makefile:
```bash
make docker-up
```
Or via PowerShell:
```powershell
.\scripts\manage.ps1 docker-up
```

### 2. Build and Run the API
```bash
dotnet run --project src/TaskForge.Api/TaskForge.Api.csproj
```
Or via Makefile:
```bash
make run
```
Or via PowerShell:
```powershell
.\scripts\manage.ps1 run
```

### 3. Run Tests
```bash
dotnet test TaskForge.slnx
```
Or via Makefile:
```bash
make test
```

## Endpoints & Documentation

- **OpenAPI 3.1 Specification**: `http://localhost:8080/openapi/v1.json`
- **Health Check**: `GET http://localhost:8080/health`

## Key Features

1. **Schema-per-Tenant Isolation**: Each tenant data is isolated in a dedicated PostgreSQL schema (`tenant_<tenant_id>`).
2. **CQRS & MediatR**: Use cases encapsulated in commands/queries with automatic validation & performance logging behaviors.
3. **Riok.Mapperly**: Compile-time source generated DTO mappers.
4. **JWT Authentication & Refresh Tokens**: Includes ASP.NET Core 10 authorization metrics.
5. **Native OpenAPI 3.1**: Powered by `.NET 10` native `Microsoft.AspNetCore.OpenApi`.
