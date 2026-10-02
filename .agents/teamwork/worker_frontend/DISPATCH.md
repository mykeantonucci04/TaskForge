## 2026-09-28T19:50:02Z

You are worker_frontend (Role: Frontend Developer).
Your working directory is: c:\Users\AntonucciMichele\TaskForge\.agents\teamwork\worker_frontend
Mandatory reading: Read c:\Users\AntonucciMichele\TaskForge\.agents\teamwork\ORIGINAL_REQUEST.md before starting.
Also read:
- c:\Users\AntonucciMichele\TaskForge\.agents\teamwork\orchestrator_1\PROJECT.md
- c:\Users\AntonucciMichele\TaskForge\.agents\teamwork\explorer_frontend\handoff.md (contains exact code snippets, models, templates, and styles).
- c:\Users\AntonucciMichele\TaskForge\.agents\teamwork\worker_backend\handoff.md (contains backend implementation details and DTO shapes).

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Write Ownership:
You have exclusive write ownership of:
- frontend/src/app/core/models/auth.models.ts
- frontend/src/app/core/services/auth.service.ts
- frontend/src/app/core/interceptors/jwt.interceptor.ts
- frontend/src/app/features/auth/login/login.component.ts
- frontend/src/app/features/auth/register-tenant/register-tenant.component.ts
- frontend/src/app/features/auth/register-tenant/register-tenant.component.spec.ts (new or updated)
- frontend/src/app/features/auth/login/login.component.spec.ts (new or updated)
- frontend/src/app/core/services/auth.service.spec.ts

Tasks:
1. In `frontend/src/app/core/models/auth.models.ts`:
   - Update `RegisterTenantRequest` to include `slug`, `adminFirstName`, `adminLastName`, `adminEmail`, `adminPassword`, `confirmPassword`, with backward-compatible optional fields.
   - Update `AuthResponse` to include `accessToken`, `companyName`, `adminFullName`, `user` object, while retaining backward compatibility for `token` and `tenantName`.
2. In `frontend/src/app/core/services/auth.service.ts`:
   - Add `deriveSlug(name: string): string` to generate URL-safe slug matching `^[a-z0-9]+(-[a-z0-9]+)*$` (3-50 chars).
   - In `registerTenant()`, ensure payload defaults `adminFirstName: 'Admin'`, `adminLastName: 'Admin'`, and derived `slug`.
   - In `handleAuthSuccess()`, support `res.accessToken || res.token`, `res.companyName || res.tenantName`, `res.user?.id || res.adminUserId || res.userId`, `res.user?.fullName || res.adminFullName`.
3. In `frontend/src/app/core/interceptors/jwt.interceptor.ts`:
   - Exclude `/auth/login` and `/auth/register-tenant` from calling `authService.logout()` when receiving HTTP 401.
4. In `frontend/src/app/features/auth/login/login.component.ts`:
   - Handle HTTP 401 by extracting `err.error?.detail || err.error?.message || 'Credenziali non valide o email non registrata.'` and display in `errorMessage` signal for the alert box.
5. In `frontend/src/app/features/auth/register-tenant/register-tenant.component.ts`:
   - Remove "Nome" and "Cognome" input fields from template.
   - Add "Conferma Password" field to form and template.
   - Add `passwordMatchValidator` ensuring `adminPassword === confirmPassword`.
   - Add interactive toggle buttons ("Mostra" / "Nascondi") with `type="button"` for both `adminPassword` and `confirmPassword` using signals `showPassword` and `showConfirmPassword`.
   - Add `strongPasswordValidator` enforcing: min 8 characters, >=1 uppercase, >=1 lowercase, >=1 digit, >=1 special character `[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]` with clear error messages.
   - In submit handler, automatically derive slug and supply `adminFirstName: 'Admin'` and `adminLastName: 'Admin'`.
   - Add CSS for `.password-input-group` and `.password-toggle-btn`.
6. Write unit tests:
   - Create or update `register-tenant.component.spec.ts` verifying all requirements (no name inputs, confirm password field, show/hide toggle buttons, strong password validator, slug and admin names in payload).
   - Create or update `login.component.spec.ts` verifying 401 error message display in alert box.
   - Update `auth.service.spec.ts` if needed to ensure all auth service tests pass.
7. Verification:
   - In `frontend/`, run `npm test -- --watch=false` and ensure 100% tests pass.
   - In `frontend/`, run `npm run build` and ensure 0 compilation errors.
8. Git commit:
   - Stage and commit changes on branch `feature/auth-and-tenant-fixes` with a clear commit message. NEVER merge into main.
9. Report:
   - Write completion report to `c:\Users\AntonucciMichele\TaskForge\.agents\teamwork\worker_frontend\handoff.md`.
10. Send message to orchestrator when finished.
