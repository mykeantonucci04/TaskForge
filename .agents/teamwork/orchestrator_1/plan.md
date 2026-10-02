# Execution Plan: TaskForge Auth & Tenant Registration Fixes

## Objectives
Coordinate specialized subagents to resolve all 5 authentication and tenant registration issues across .NET backend and Angular frontend, adhering to git branching on `feature/auth-and-tenant-fixes` with zero merges to main and complete test & audit pass.

## Phases & Steps

### Phase 0: Survey & Branch Setup (Milestone 0)
- Dispatch Git Manager Worker (`worker_git_setup`) to:
  - Check current branch, status, commits.
  - Create and switch to branch `feature/auth-and-tenant-fixes`.
  - Ensure working tree is clean.
- Dispatch 2 Explorers in parallel:
  - `explorer_backend`: Investigate backend Auth & Tenant codebase (.NET): Login handling (where 500 happens on missing email/DomainException), RegisterTenantCommand, Validators, AdminFirstName/LastName handling, Tenant Slug and Schema provisioning, existing unit/integration tests.
  - `explorer_frontend`: Investigate frontend Angular codebase: `RegisterTenantComponent`, `LoginComponent`, `AuthService`, ReactiveForms, password visibility toggles, slug generation, Vitest test suites (`*.spec.ts`).
- Review reports and finalize implementation roadmap.

### Phase 1: Backend Fixes (Milestone 1)
- Dispatch Backend Worker (`worker_backend`) to:
  - Fix login handler: return 401 Unauthorized ProblemDetails instead of throwing unhandled 500/DomainException when email is not found or credentials invalid.
  - Fix tenant registration: default AdminFirstName and AdminLastName to "Admin" if not provided or set defaults.
  - Update `RegisterTenantCommandValidator` with strict password complexity policy (min 8 chars, 1 upper, 1 lower, 1 digit, 1 special).
  - Verify and fix tenant creation, slug validation, and schema provisioning.
  - Run `dotnet test` and ensure existing and new tests pass.
- Dispatch Reviewer (`reviewer_backend`) and Challenger (`challenger_backend`) to verify backend changes.
- Dispatch Git Worker to commit backend changes incrementally to `feature/auth-and-tenant-fixes`.

### Phase 2: Frontend Fixes (Milestone 2)
- Dispatch Frontend Worker (`worker_frontend`) to:
  - Update `RegisterTenantComponent`:
    - Remove "Nome" and "Cognome" input fields; default internally to "Admin" / "Admin".
    - Add "Conferma Password" field.
    - Add interactive toggle buttons ("Mostra" / "Nascondi") for both password fields.
    - Implement custom validator: password === confirmPassword.
    - Implement strong password validator (min 8 chars, 1 upper, 1 lower, 1 digit, 1 special symbol) with clear error messages.
    - Automatically derive tenant slug from companyName (e.g., lowercase kebab-case).
  - Update `LoginComponent`:
    - Display clear, user-friendly error in alert box when backend returns 401.
  - Update `AuthService`:
    - Align DTO mapping with backend response (`accessToken`, `tenantId`, `companyName`).
  - Run `npm test -- --watch=false` and `npm run build` and update tests as necessary.
- Dispatch Reviewer (`reviewer_frontend`) and Challenger (`challenger_frontend`) to verify frontend changes.
- Dispatch Git Worker to commit frontend changes incrementally to `feature/auth-and-tenant-fixes`.

### Phase 3: Comprehensive Testing & Quality Audit (Milestone 3)
- Dispatch Test & Quality Worker / Test Writer to run all test suites:
  - Backend: `dotnet test` (100% passing).
  - Frontend: `npm test -- --watch=false` (100% passing).
  - Frontend build: `npm run build` (0 errors).
- Dispatch Forensic Auditor (`teamwork_preview_auditor`) to verify:
  - No cheating or hardcoded mock bypasses.
  - Real database / schema provisioning logic.
  - Real Angular forms and backend validators.

### Phase 4: Git Finalization & Verification (Milestone 4)
- Dispatch Git Worker to:
  - Review git status and git log.
  - Ensure all commits are on `feature/auth-and-tenant-fixes`.
  - Push branch to remote.
  - Verify `main` branch was NOT touched or merged into.
- Record final verification and report to Sentinel.
