## 2026-09-28T19:37:37Z
You are worker_backend (Role: Backend Developer).
Your working directory is: c:\Users\AntonucciMichele\TaskForge\.agents\teamwork\worker_backend
Mandatory reading: Read c:\Users\AntonucciMichele\TaskForge\.agents\teamwork\ORIGINAL_REQUEST.md before starting.
Also read:
- c:\Users\AntonucciMichele\TaskForge\.agents\teamwork\orchestrator_1\PROJECT.md
- c:\Users\AntonucciMichele\TaskForge\.agents\teamwork\explorer_backend\handoff.md (contains exact line numbers, code snippets, and instructions).

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Write Ownership:
You have exclusive write ownership of:
- src/TaskForge.Domain/Common/Email.cs
- src/TaskForge.Application/Features/Auth/AuthFeature.cs
- src/TaskForge.Api/Endpoints/Endpoints.cs
- src/TaskForge.Application/Features/Tenants/RegisterTenant.cs
- tests/TaskForge.Application.UnitTests/AuthUnitTests.cs (or appropriate unit test files in tests/)

Tasks:
1. Implement `Email.TryCreate` in `src/TaskForge.Domain/Common/Email.cs` so invalid email strings do not throw unhandled `DomainException`.
2. In `src/TaskForge.Application/Features/Auth/AuthFeature.cs`, update `LoginCommandHandler`:
   - Use `Email.TryCreate(request.Email, out var emailVo)`. If false, return `Error.Unauthorized("Auth.InvalidCredentials", "Credenziali non valide o email non registrata.")`.
   - When user is null, inactive, or password verify fails, return `Error.Unauthorized("Auth.InvalidCredentials", "Credenziali non valide o email non registrata.")`.
3. In `src/TaskForge.Api/Endpoints/Endpoints.cs`, update `MapErrorResult`:
   - For `ErrorType.Unauthorized`, return `Microsoft.AspNetCore.Http.Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized", detail: string.IsNullOrWhiteSpace(error.Description) ? "Credenziali non valide o email non registrata." : error.Description)`. Ensure other error types return proper ProblemDetails with details instead of empty bodies.
4. In `src/TaskForge.Application/Features/Tenants/RegisterTenant.cs`:
   - Default `AdminFirstName = "Admin"` and `AdminLastName = "Admin"` in `RegisterTenantCommand`.
   - In `RegisterTenantCommandValidator`, remove `.NotEmpty()` from `AdminFirstName` and `AdminLastName`.
   - Enforce strong password complexity in `RegisterTenantCommandValidator`:
     - Minimum 8 characters
     - At least 1 uppercase letter (`[A-Z]`)
     - At least 1 lowercase letter (`[a-z]`)
     - At least 1 digit (`[0-9]`)
     - At least 1 special character (`[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]`)
   - In `RegisterTenantCommandHandler`, fallback admin names to "Admin" if null or whitespace.
5. Add unit tests in `tests/TaskForge.Application.UnitTests/AuthUnitTests.cs` (or create it) covering:
   - Login with non-existent user returns 401 Unauthorized with description.
   - Login with invalid password returns 401 Unauthorized.
   - Login with malformed email returns 401 Unauthorized without throwing DomainException.
   - Tenant registration with omitted/defaulted admin names passes validation.
   - Password validation correctly rejects passwords missing uppercase, lowercase, digit, or special characters, and accepts valid complex passwords.
6. Run `dotnet test` across the solution and ensure 100% of tests pass.
7. Stage and commit your changes to git on `feature/auth-and-tenant-fixes` with a clear commit message. NEVER merge into main.
8. Write your completion report in `c:\Users\AntonucciMichele\TaskForge\.agents\teamwork\worker_backend\handoff.md`.
9. Send a message to the orchestrator when finished.
