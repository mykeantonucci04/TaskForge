# TaskForge Enterprise Architecture Specification

## Overview

**TaskForge** is an enterprise-grade multi-tenant project management SaaS backend built with **.NET 10** following **Clean Architecture** and **Domain-Driven Design (DDD)** principles.

## Layer Structure & Dependencies

```
┌─────────────────────────────────────────────────────────┐
│                    TaskForge.Api                        │
│   (ASP.NET Core 10 Web API, JWT Auth, Tenant Middleware) │
└───────────┬─────────────────────────────────┬───────────┘
            │                                 │
            ▼                                 ▼
┌───────────────────────┐         ┌───────────────────────┐
│TaskForge.Infrastructure│         │ TaskForge.Application │
│(EF Core 10, PostgreSQL│───────> │ (CQRS, MediatR, DTOs, │
│ S3 Storage, SignalR)  │         │ FluentValidation)     │
└───────────┬───────────┘         └───────────┬───────────┘
            │                                 │
            └────────────────┬────────────────┘
                             ▼
              ┌──────────────────────────────┐
              │      TaskForge.Domain        │
              │  (Entities, Enums, Events)   │
              └──────────────┬───────────────┘
                             │
                             ▼
              ┌──────────────────────────────┐
              │      TaskForge.Shared        │
              │(Result Pattern, Exceptions)  │
              └──────────────────────────────┘
```

### Layer Responsibilities

1. **TaskForge.Domain**: Core enterprise business logic and domain model. Zero external dependencies. Defines aggregates (`Tenant`, `TaskItem`, `Workspace`, `User`, etc.), domain events, value objects, and repository interfaces.
2. **TaskForge.Application**: Implements use cases via CQRS (`MediatR`). Handles command validation (`FluentValidation`), object mapping (`Riok.Mapperly`), and defines interfaces for infrastructure services (`ITenantContext`, `ICurrentUserService`, `IStorageService`).
3. **TaskForge.Infrastructure**: Technical implementations. EF Core 10 DbContext with **Schema-per-Tenant dynamic resolution** on PostgreSQL (`Npgsql`), concrete repositories, SignalR notification hubs, S3 file storage adapter, and SMTP email services.
4. **TaskForge.Api**: Entry point. ASP.NET Core 10 Web API with Minimal APIs, native OpenAPI 3.1, `TenantResolutionMiddleware` (reads claim/header to set tenant scope), JWT authentication with refresh tokens, and health checks at `/health`.
5. **TaskForge.Shared**: Shared kernel containing the `Result<T>` pattern, custom exception types, common constants, and utility extension methods.

## Multi-Tenancy Strategy: Schema-Per-Tenant

- **Database Engine**: PostgreSQL 16+
- **Isolation Level**: Schema-per-tenant (`tenant_xxx` schemas) with shared metadata in `public.tenants`.
- **Resolution Flow**:
  1. Client sends request with JWT Bearer Token containing `tenant_id` & `tenant_schema` claim OR `X-Tenant-ID` header.
  2. `TenantResolutionMiddleware` extracts the tenant info and sets `ITenantContext` in DI scope.
  3. `TaskForgeDbContext` reads `ITenantContext.SchemaName` and configures EF Core dynamic schema (`ModelBuilder.HasDefaultSchema(...)`).
