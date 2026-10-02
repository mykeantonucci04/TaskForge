# Project: TaskForge Auth & Tenant Registration Fixes

## Architecture
- Backend: .NET 8/9 Clean Architecture (WebAPI, Application, Domain, Infrastructure). EF Core, MediatR, FluentValidation.
- Frontend: Angular 21 Standalone Components, Reactive Forms, Tailwind / CSS, RxJS.
- Database & Multi-tenancy: Schema/Tenant provisioning, JWT Authentication.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | R1: Password Confirmation & Toggle | RegisterTenantComponent: add confirm password field, show/hide toggle buttons, match validation | M2 | ORIGINAL_REQUEST |
| 2 | R2: Login 401 & Friendly UI | Backend returns 401 with problem details on invalid/non-existent user; Frontend displays message in alert box | M1, M2 | ORIGINAL_REQUEST |
| 3 | R3: Standard Admin User | Default AdminFirstName and AdminLastName to "Admin"; remove name/surname from UI; update DTOs | M1, M2 | ORIGINAL_REQUEST |
| 4 | R4: Strong Password Policy | Min 8 chars, 1 uppercase, 1 lowercase, 1 digit, 1 special character in FluentValidation & Angular ReactiveForm | M1, M2 | ORIGINAL_REQUEST |
| 5 | R5: Tenant Creation & Slug Alignment | Derive slug from companyName on frontend, align AuthService DTOs with backend response, ensure schema provisioning succeeds | M1, M2 | ORIGINAL_REQUEST |
| 6 | R6: Git Workflow | feature/auth-and-tenant-fixes branch, incremental commits, push, NO merge to main | M0, M4 | ORIGINAL_REQUEST |

## Milestones
| # | Name | Scope | Dependencies | Status | Key Outputs |
|---|------|-------|-------------|--------|-------------|
| M0 | Git Setup & Codebase Survey | Create feature/auth-and-tenant-fixes; explore backend and frontend auth/tenant code | none | DONE | Branch established, explorer handoffs delivered |
| M1 | Backend Fixes | Implement R2 (401 on missing user), R3 (Admin defaults), R4 (Strong password validator), R5 (Tenant creation & schema provisioning) | M0 | DONE | Commit 7812477, 69 unit tests passed |
| M2 | Frontend Fixes | Implement R1 (confirm password + toggle), R2 (friendly login error), R3 (hidden admin names), R4 (strong password validator), R5 (slug derivation + response mapping) | M0, M1 | DONE | Commit 275ddc8, 30 Vitest tests passed, 0 build errors |
| M3 | Test & Quality Audit | Run dotnet test, npm test, npm run build, adversarial checks, and Forensic Audit | M1, M2 | DONE | Reviewer APPROVE, Challenger APPROVE, Auditor CLEAN |
| M4 | Git Finalization | Verify commits on feature/auth-and-tenant-fixes, push to remote, ensure main is untouched | M3 | DONE | Pushed to origin/feature/auth-and-tenant-fixes; main untouched |

## Interface Contracts
### Auth / Login: POST /api/auth/login
- Request: `{ email, password }`
- Response 200: `{ accessToken, refreshToken, user: { id, email, ... } }`
- Response 401: ProblemDetails `{ status: 401, title: "Unauthorized", detail: "Credenziali non valide o email non registrata." }` (No 500 unhandled DomainException)

### Tenant Registration: POST /api/tenants / /api/auth/register-tenant
- Request: `{ companyName, slug, adminEmail, password, confirmPassword, adminFirstName: "Admin", adminLastName: "Admin" }`
- Slug generation: lowercase alphanumeric with hyphens derived from companyName (e.g. "Acme S.p.A." -> "acme-spa")
- Password policy: >=8 chars, >=1 upper, >=1 lower, >=1 digit, >=1 special character
- Response 200/201: `{ accessToken, tenantId, companyName, ... }`

## Code Layout
- Backend:
  - `src/TaskForge.Domain/Common/Email.cs`
  - `src/TaskForge.Application/Features/Auth/AuthFeature.cs`
  - `src/TaskForge.Application/Features/Tenants/RegisterTenant.cs`
  - `src/TaskForge.Api/Endpoints/Endpoints.cs`
  - `tests/TaskForge.Application.UnitTests/AuthUnitTests.cs`
- Frontend:
  - `frontend/src/app/core/models/auth.models.ts`
  - `frontend/src/app/core/services/auth.service.ts`
  - `frontend/src/app/core/interceptors/jwt.interceptor.ts`
  - `frontend/src/app/features/auth/login/login.component.ts`
  - `frontend/src/app/features/auth/register-tenant/register-tenant.component.ts`
  - Tests: `register-tenant.component.spec.ts`, `login.component.spec.ts`, `auth.service.spec.ts`
