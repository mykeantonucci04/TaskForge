# Original User Request

## 2026-09-28T19:30:03Z

Specialized team of agents: Git Manager (branching, commits, push, NO merge on main), Backend Engineer (Auth/Login/Tenant fixes), Frontend Developer (Password confirmation, toggle visibility, Admin default, strong password validation), Test & Quality Auditor (Vitest + dotnet test verification).

Resolve all 5 reported authentication and tenant registration issues across backend (.NET) and frontend (Angular 21) in TaskForge, maintaining strict branch management without merging to main.

Working directory: c:\Users\AntonucciMichele\TaskForge
Integrity mode: development

## Requirements

### R1. Password Confirmation and Visibility Toggle (Frontend)
- On the tenant registration page (RegisterTenantComponent), add a "Conferma Password" (Password Confirmation) field.
- Provide interactive toggle buttons ("Mostra" / "Nascondi") to view or mask both the password and confirmation password in plaintext.
- Enforce validation that password === confirmPassword before enabling or submitting the form.

### R2. Proper Login Error Handling - No 500 (Backend & Frontend)
- When attempting to login with an unregistered email or non-existent credentials, the backend must return a proper client error (401 Unauthorized with problem details and informative message) rather than crashing or throwing an unhandled 500 Internal Server Error.
- Ensure email format validation and database lookup handle non-existent users gracefully without unhandled DomainException.
- The frontend login page must display a clear, user-friendly error message indicating that the email is not registered or credentials are invalid.

### R3. Standard Admin User on Tenant Registration (Backend & Frontend)
- Do not require or ask the user for "Nome" and "Cognome" during tenant registration. Default them automatically to "Admin" and "Admin".
- Remove the name and surname input fields from the registration form in the UI.
- The backend and frontend DTOs must automatically supply AdminFirstName = "Admin" and AdminLastName = "Admin".

### R4. Strong Password Policy (Backend & Frontend)
- Enforce a strict password complexity policy:
  - Minimum 8 characters
  - At least 1 uppercase letter ([A-Z])
  - At least 1 lowercase letter ([a-z])
  - At least 1 number ([0-9])
  - At least 1 special symbol / punctuation character ([!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?])
- Validate this policy in both RegisterTenantCommandValidator on the backend and in the Angular ReactiveForm validator on the frontend with descriptive error messages.

### R5. Fix Tenant Creation Issue (Backend & Frontend Alignment)
- Fix the issue preventing tenant registration from completing successfully:
  - Ensure the frontend sends all expected fields matching RegisterTenantCommand, including generating or deriving the required slug (URL identifier) from companyName (e.g. acme-spa).
  - Align AuthService.registerTenant DTO mapping with backend response (accessToken, tenantId, companyName).
  - Verify that schema provisioning and database persistence succeed without exceptions.

### R6. Git Workflow
- Create a dedicated feature branch: feature/auth-and-tenant-fixes
- Commit changes incrementally with clear messages.
- CRITICAL: Never merge into main.

## Acceptance Criteria
- [ ] Feature branch feature/auth-and-tenant-fixes created and committed (no merge to main).
- [ ] Registration form has a "Conferma Password" field and checks password match.
- [ ] Both password fields have a show/hide toggle.
- [ ] Strong password validator requires min 8 chars, 1 uppercase, 1 lowercase, 1 digit, 1 special character on both FE and BE.
- [ ] Registration form only asks for: Company Name, Admin Email, Password, Confirm Password (First/Last name hidden and defaulted to Admin).
- [ ] Tenant registration payload includes a valid tenant slug derived from company name.
- [ ] Tenant registration succeeds, receives tokens, and redirects to dashboard.
- [ ] Login with non-existent email returns HTTP 401 with informative detail without any 500 error.
- [ ] Login UI displays this message clearly in the alert box.
- [ ] dotnet test passes with zero failing tests.
- [ ] npm test -- --watch=false passes with 100% success rate.
- [ ] npm run build compiles with zero errors.
