# ADR 0001: Multi-Tenancy Strategy - Schema-per-Tenant on PostgreSQL

## Status
Accepted

## Context
TaskForge is a SaaS project management platform for enterprise clients. Data isolation, security compliance, compliance with GDPR/SOC2, and performance predictability are critical requirements.

## Decision
We decided to adopt a **Schema-per-Tenant** isolation model on PostgreSQL:
- Metadata regarding all tenants is stored in a single table `public.tenants`.
- Each tenant's workspaces, tasks, projects, and users reside in a dedicated PostgreSQL schema (`tenant_<tenant_id>`).
- EF Core 10 `TaskForgeDbContext` receives `ITenantContext` via constructor injection and sets `ModelBuilder.HasDefaultSchema(tenantContext.SchemaName)`.

## Consequences
### Positive
- High data isolation and privacy protection.
- Simplified per-tenant backup and restore capabilities.
- Prevents accidental cross-tenant data leakage in SQL queries.

### Negative
- Higher database migration complexity across hundreds of schemas.
- Slightly higher overhead during schema setup upon onboarding a new tenant.
