# Handoff Report: Frontend Exploration for Auth & Tenant Fixes

## 1. Observation

### Baseline Testing Execution
- **Command executed**: `npm test -- --watch=false` in directory `c:\Users\AntonucciMichele\TaskForge\frontend`.
- **Result**: Terminal command execution via `run_command` prompted for user confirmation and timed out after waiting 60s (`permission check failed for command "npm test -- --watch=false": Permission prompt for action 'command' timed out waiting for user response`). Per protocol, exploration continued using static analysis, source code parsing, and AST inspection.
- **Project Structure**:
  - Angular 21 with `@angular/build:unit-test` using Vitest (`angular.json` line 68, `package.json` line 31).
  - Standalone components used exclusively across the application (`app.config.ts`, `app.routes.ts`, `login.component.ts`, `register-tenant.component.ts`).
  - Unit test files found:
    - `src/app/app.spec.ts`
    - `src/app/core/services/auth.service.spec.ts`
    - `src/app/core/services/project.service.spec.ts`
    - `src/app/core/services/task.service.spec.ts`
    - `src/app/core/services/workspace.service.spec.ts`
    - Note: Neither `LoginComponent` nor `RegisterTenantComponent` currently has an associated `.spec.ts` file.

---

### Detailed Codebase Observations by Requirement

#### R1 & R3 & R4 & R5: `RegisterTenantComponent`
- **File**: `frontend/src/app/features/auth/register-tenant/register-tenant.component.ts`
- **Current Form Group (lines 227-233)**:
  ```typescript
  readonly form = this.fb.nonNullable.group({
    companyName: ['', [Validators.required, Validators.minLength(2)]],
    firstName: ['', [Validators.required]],
    lastName: ['', [Validators.required]],
    adminEmail: ['', [Validators.required, Validators.email]],
    adminPassword: ['', [Validators.required, Validators.minLength(8)]]
  });
  ```
- **Current Template (lines 50-80, lines 97-110)**:
  - Form explicitly displays and requires "Nome" (`firstName`) and "Cognome" (`lastName`) in a `.form-row` grid (lines 50-80).
  - `adminPassword` is a plain `<input type="password">` without any show/hide toggle button (lines 97-106).
  - There is NO "Conferma Password" (`confirmPassword`) field anywhere in the template or form definition.
  - Password validation is only `Validators.minLength(8)` (line 232). There is no check for uppercase, lowercase, digit, or special characters.
  - Form submission (lines 244-245) executes:
    ```typescript
    this.authService.registerTenant(this.form.getRawValue()).subscribe({ ... });
    ```
    This sends `{ companyName, firstName, lastName, adminEmail, adminPassword }`.
    There is no `slug`, and the admin names are keyed as `firstName`/`lastName` instead of `adminFirstName`/`adminLastName`.

#### R5: Backend Interface vs Frontend Alignment
- **Backend File**: `src/TaskForge.Application/Features/Tenants/RegisterTenant.cs`
- **Backend Command (lines 12-19)**:
  ```csharp
  public record RegisterTenantCommand(
      string CompanyName,
      string Slug,
      string AdminFirstName,
      string AdminLastName,
      string AdminEmail,
      string AdminPassword,
      TenantPlan Plan = TenantPlan.Free) : ICommand<RegisterTenantResponseDto>;
  ```
- **Backend Validator (lines 35-67)**:
  - Requires `Slug` (min 3, max 50 chars, regex `^[a-z0-9]+(-[a-z0-9]+)*$`).
  - Requires `AdminFirstName` and `AdminLastName`.
  - Requires `AdminPassword` min 8 chars.
- **Backend Response (lines 21-33)**:
  ```csharp
  public record RegisterTenantResponseDto(
      Guid TenantId,
      string CompanyName,
      string Slug,
      string SchemaName,
      TenantPlan Plan,
      Guid AdminUserId,
      string AdminEmail,
      string AdminFullName,
      string Role,
      string AccessToken,
      string RefreshToken,
      DateTime RefreshTokenExpiryUtc);
  ```

#### R5: Frontend DTOs & Service
- **File**: `frontend/src/app/core/models/auth.models.ts` (lines 6-26):
  ```typescript
  export interface RegisterTenantRequest {
    companyName: string;
    adminEmail: string;
    adminPassword: string;
    firstName: string;
    lastName: string;
  }

  export interface AuthResponse {
    token: string;
    refreshToken?: string;
    expiresAt?: string;
    userId?: string;
    adminUserId?: string;
    email?: string;
    adminEmail?: string;
    firstName?: string;
    lastName?: string;
    tenantId: string;
    tenantName?: string;
  }
  ```
- **File**: `frontend/src/app/core/services/auth.service.ts` (lines 49-70):
  ```typescript
  private handleAuthSuccess(res: AuthResponse): void {
    const token = res.token;
    const userId = res.userId || res.adminUserId || '';
    const email = res.email || res.adminEmail || '';
    const fullName = res.firstName && res.lastName
      ? `${res.firstName} ${res.lastName}`
      : (res.tenantName ? `Admin (${res.tenantName})` : email);
    const tenantId = res.tenantId;
    const tenantName = res.tenantName || 'Workspace';
    ...
  ```
  - `res.token` is undefined for backend responses, because backend returns `accessToken` (both in login `AuthResponseDto` and `RegisterTenantResponseDto`).
  - For login: backend returns `{ accessToken, refreshToken, user: { id, email, fullName, tenantId, ... } }`. Currently `res.user` is not inspected at all by `handleAuthSuccess`.
  - For tenant registration: backend returns `{ accessToken, tenantId, companyName, adminFullName, adminUserId, adminEmail }`. `res.tenantName` is undefined (backend sends `companyName`).

#### R2: `LoginComponent` and Interceptors
- **File**: `frontend/src/app/features/auth/login/login.component.ts` (lines 188-198):
  ```typescript
  this.authService.login(this.form.getRawValue()).subscribe({
    next: () => {
      this.isLoading.set(false);
      this.router.navigate(['/dashboard']);
    },
    error: (err) => {
      this.isLoading.set(false);
      const msg = err.error?.detail || err.error?.message || 'Credenziali non valide o errore di connessione.';
      this.errorMessage.set(msg);
    }
  });
  ```
- **File**: `frontend/src/app/core/interceptors/jwt.interceptor.ts` (lines 26-33):
  ```typescript
  return next(clonedRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        authService.logout();
      }
      return throwError(() => error);
    })
  );
  ```
  - When login fails with 401 Unauthorized, `jwtInterceptor` catches it and calls `authService.logout()` (which calls `router.navigate(['/auth/login'])`), potentially creating unnecessary navigation events or race conditions during login attempt.
  - In `login.component.ts`, if backend returns 401 without ProblemDetails JSON body (as `Endpoints.cs` line 309 currently did with `Results.Unauthorized()`), `err.error` is empty and fallback `'Credenziali non valide o errore di connessione.'` is shown.

---

## 2. Logic Chain

1. **R1 (Password Confirmation & Visibility Toggle)**:
   - Observation: `RegisterTenantComponent` form group only contains `adminPassword`, no `confirmPassword`. Template does not provide a confirmation input or toggle button.
   - Requirement: User must enter password twice; form must validate `password === confirmPassword` before enabling submit; user must be able to toggle visibility between plaintext and masked with "Mostra" / "Nascondi".
   - Inference:
     - Add `confirmPassword: ['', [Validators.required]]` to `form`.
     - Implement a cross-field validator `passwordMatchValidator` on the `FormGroup`.
     - Introduce two signals: `showPassword = signal(false)` and `showConfirmPassword = signal(false)`.
     - Add interactive toggle `<button type="button" class="password-toggle-btn">` dynamically changing input `[type]="showPassword() ? 'text' : 'password'"` and text `{{ showPassword() ? 'Nascondi' : 'Mostra' }}`.
     - Note: Button must be `type="button"` to avoid accidentally triggering form submission.

2. **R2 (Login Error Handling)**:
   - Observation: Backend minimal API mapped 401 via `Results.Unauthorized()`, which outputs an empty response body without problem details.
   - Requirement: Backend returns 401 ProblemDetails with informative detail; frontend login page displays a clear, user-friendly error message in the alert box indicating that the email is not registered or credentials are invalid.
   - Inference:
     - On frontend `LoginComponent`, check `err.status === 401` specifically.
     - Priority extraction: `err.error?.detail || err.error?.message || err.error?.error?.message || 'Credenziali non valide o email non registrata.'`.
     - In `jwt.interceptor.ts`, exclude authentication routes (`/auth/login`, `/auth/register-tenant`) from auto-logout redirect on 401.

3. **R3 (Standard Admin User)**:
   - Observation: UI currently renders "Nome" and "Cognome" fields, which are required in the reactive form.
   - Requirement: Do not ask user for Nome and Cognome; remove fields from UI; default them automatically to "Admin" and "Admin" in both frontend and backend DTOs.
   - Inference:
     - Remove the HTML block `<div class="form-row">...</div>` containing `firstName` and `lastName` from `register-tenant.component.ts`.
     - In `register-tenant.component.ts`, assemble registration payload supplying `adminFirstName: 'Admin'` and `adminLastName: 'Admin'`.

4. **R4 (Strong Password Policy)**:
   - Observation: Angular form only validated `Validators.minLength(8)`.
   - Requirement: Enforce minimum 8 characters, at least 1 uppercase `[A-Z]`, at least 1 lowercase `[a-z]`, at least 1 digit `[0-9]`, and at least 1 special character `[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]`. Provide descriptive error messages.
   - Inference:
     - Create a custom Angular validator function `strongPasswordValidator`.
     - Return structured error `{ strongPassword: { hasMinLength, hasUpperCase, hasLowerCase, hasDigit, hasSpecial } }` or null.
     - In the template, show: `"La password deve contenere almeno 8 caratteri, una lettera maiuscola, una minuscola, un numero e un carattere speciale."` when touched and invalid.

5. **R5 (Tenant Creation & Slug Alignment)**:
   - Observation:
     - Backend `RegisterTenantCommand` requires `Slug`, `AdminFirstName`, `AdminLastName`, `AdminEmail`, `AdminPassword`.
     - Backend returns `RegisterTenantResponseDto` with `AccessToken`, `TenantId`, `CompanyName`, `AdminFullName`, `AdminUserId`.
     - Frontend was omitting `Slug`, sending `firstName`/`lastName`, and `AuthService.handleAuthSuccess` was looking for `res.token` and `res.tenantName`, which were both undefined.
   - Requirement: Frontend sends all expected fields matching `RegisterTenantCommand`, generating or deriving the required slug from `companyName`; aligns `AuthService.registerTenant` response mapping.
   - Inference:
     - Implement `generateSlug(companyName)` utility adhering to backend regex `^[a-z0-9]+(-[a-z0-9]+)*$` (length 3-50, lowercase alphanumeric, hyphens).
     - Update `RegisterTenantRequest` in `auth.models.ts` with `slug`, `adminFirstName`, `adminLastName`.
     - Update `AuthResponse` in `auth.models.ts` to accommodate `accessToken`, `companyName`, `adminFullName`, `user` object.
     - Update `handleAuthSuccess` in `auth.service.ts`:
       - `token = res.accessToken || res.token || ''`
       - `tenantName = res.companyName || res.tenantName || res.user?.schemaName || 'Workspace'`
       - `fullName = res.user?.fullName || res.adminFullName || ...`
       - `userId = res.user?.id || res.adminUserId || res.userId || ''`
       - `tenantId = res.user?.tenantId || res.tenantId || ''`

---

## 3. Caveats

1. **Terminal Command Permission Timeout**: `run_command` timed out waiting for user approval prompt. Baseline test execution was therefore established via complete static inspection of all 5 existing `.spec.ts` files, `angular.json`, and `tsconfig.spec.json`.
2. **Existing Unit Test Mock Shape**: `src/app/core/services/auth.service.spec.ts` currently mocks `{ token, userId, email, firstName, lastName, tenantId, tenantName }`. The updated `handleAuthSuccess` implementation is intentionally backwards-compatible (`res.accessToken || res.token`) so that existing unit tests will continue to pass seamlessly while supporting the real backend DTOs.
3. **Admin User Profile Display**: Since `adminFirstName` and `adminLastName` are defaulted to `"Admin"` and `"Admin"`, `adminFullName` in backend is `"Admin Admin"`. The app shell avatar will show initials `"AA"`, which is expected behavior for the default admin.

---

## 4. Conclusion & Proposed Code Modifications

The root causes for all frontend issues are identified and mapped to concise, targeted modifications across 5 frontend files:

### Proposed File 1: `src/app/core/models/auth.models.ts`
```typescript
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterTenantRequest {
  companyName: string;
  slug: string;
  adminFirstName: string;
  adminLastName: string;
  adminEmail: string;
  adminPassword: string;
  confirmPassword?: string;
  firstName?: string; // backwards compatibility
  lastName?: string;  // backwards compatibility
  plan?: number;
}

export interface AuthResponse {
  accessToken?: string;
  token?: string; // backwards compatibility with existing unit test mocks
  refreshToken?: string;
  refreshTokenExpiryUtc?: string;
  expiresAt?: string;
  tenantId?: string;
  companyName?: string;
  tenantName?: string;
  slug?: string;
  schemaName?: string;
  role?: string;
  adminUserId?: string;
  adminEmail?: string;
  adminFullName?: string;
  userId?: string;
  email?: string;
  firstName?: string;
  lastName?: string;
  user?: {
    id: string;
    email: string;
    firstName?: string;
    lastName?: string;
    fullName?: string;
    role?: string;
    tenantId?: string;
    schemaName?: string;
  };
}

export interface CurrentUser {
  id: string;
  email: string;
  fullName: string;
  tenantId: string;
  tenantName?: string;
}
```

### Proposed File 2: `src/app/core/services/auth.service.ts`
Add `deriveSlug` helper and update `handleAuthSuccess` and `registerTenant`:
```typescript
  registerTenant(request: RegisterTenantRequest): Observable<AuthResponse> {
    const payload: RegisterTenantRequest = {
      ...request,
      adminFirstName: request.adminFirstName || request.firstName || 'Admin',
      adminLastName: request.adminLastName || request.lastName || 'Admin',
      slug: request.slug || this.deriveSlug(request.companyName)
    };
    return this.http.post<AuthResponse>(`${this.baseUrl}/register-tenant`, payload).pipe(
      tap(response => this.handleAuthSuccess(response))
    );
  }

  deriveSlug(name: string): string {
    if (!name) return 'workspace';
    let slug = name
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '');

    if (slug.length < 3) {
      slug = (slug ? `${slug}-org` : 'workspace').slice(0, 50);
    } else if (slug.length > 50) {
      slug = slug.substring(0, 50).replace(/-+$/, '');
    }
    return slug;
  }

  private handleAuthSuccess(res: AuthResponse): void {
    const token = res.accessToken || res.token || '';
    const userId = res.user?.id || res.userId || res.adminUserId || '';
    const email = res.user?.email || res.email || res.adminEmail || '';
    const fullName = res.user?.fullName
      || (res.firstName && res.lastName ? `${res.firstName} ${res.lastName}` : null)
      || res.adminFullName
      || (res.companyName ? `Admin (${res.companyName})` : null)
      || (res.tenantName ? `Admin (${res.tenantName})` : email);
    const tenantId = res.user?.tenantId || res.tenantId || '';
    const tenantName = res.companyName || res.tenantName || res.user?.schemaName || 'Workspace';

    const user: CurrentUser = {
      id: userId,
      email,
      fullName,
      tenantId,
      tenantName
    };

    if (token) {
      localStorage.setItem(TOKEN_KEY, token);
    }
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this.currentUser.set(user);
  }
```

### Proposed File 3: `src/app/core/interceptors/jwt.interceptor.ts`
```typescript
export const jwtInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const token = authService.getToken();
  const tenantId = authService.currentTenantId();

  let headers = req.headers;

  if (token) {
    headers = headers.set('Authorization', `Bearer ${token}`);
  }

  if (tenantId) {
    headers = headers.set('X-Tenant-Id', tenantId);
  }

  const clonedRequest = req.clone({ headers });

  return next(clonedRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      const isAuthEndpoint = req.url.includes('/auth/login') || req.url.includes('/auth/register-tenant');
      if (error.status === 401 && !isAuthEndpoint) {
        authService.logout();
      }
      return throwError(() => error);
    })
  );
};
```

### Proposed File 4: `src/app/features/auth/login/login.component.ts`
In `onSubmit()`:
```typescript
    this.authService.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading.set(false);
        let msg = 'Credenziali non valide o errore di connessione.';
        if (err.status === 401) {
          msg = err.error?.detail || err.error?.message || err.error?.error?.message || 'Credenziali non valide o email non registrata.';
        } else if (err.error?.detail) {
          msg = err.error.detail;
        } else if (err.error?.message) {
          msg = err.error.message;
        }
        this.errorMessage.set(msg);
      }
    });
```

### Proposed File 5: `src/app/features/auth/register-tenant/register-tenant.component.ts`
Key elements to update:
1. **Validators**:
   ```typescript
   export const strongPasswordValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
     const value = control.value;
     if (!value) return null;

     const hasMinLength = value.length >= 8;
     const hasUpperCase = /[A-Z]/.test(value);
     const hasLowerCase = /[a-z]/.test(value);
     const hasDigit = /[0-9]/.test(value);
     const hasSpecial = /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(value);

     if (!(hasMinLength && hasUpperCase && hasLowerCase && hasDigit && hasSpecial)) {
       return { strongPassword: true };
     }
     return null;
   };

   export const passwordMatchValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
     const password = control.get('adminPassword')?.value;
     const confirmPassword = control.get('confirmPassword')?.value;
     if (!password || !confirmPassword) return null;
     return password === confirmPassword ? null : { passwordMismatch: true };
   };
   ```
2. **Signals & Form Definition**:
   ```typescript
   export class RegisterTenantComponent {
     private readonly fb = inject(FormBuilder);
     private readonly authService = inject(AuthService);
     private readonly router = inject(Router);

     readonly isLoading = signal(false);
     readonly errorMessage = signal<string | null>(null);
     readonly showPassword = signal(false);
     readonly showConfirmPassword = signal(false);

     readonly form = this.fb.nonNullable.group({
       companyName: ['', [Validators.required, Validators.minLength(2)]],
       adminEmail: ['', [Validators.required, Validators.email]],
       adminPassword: ['', [Validators.required, strongPasswordValidator]],
       confirmPassword: ['', [Validators.required]]
     }, {
       validators: [passwordMatchValidator]
     });

     toggleShowPassword(): void {
       this.showPassword.update(v => !v);
     }

     toggleShowConfirmPassword(): void {
       this.showConfirmPassword.update(v => !v);
     }
     ...
   ```
3. **Template**:
   - Omit `firstName` and `lastName` form-row inputs.
   - For `adminPassword`:
     ```html
     <div class="form-group">
       <label class="form-label" for="adminPassword">Password Amministratore</label>
       <div class="password-input-group">
         <input
           id="adminPassword"
           [type]="showPassword() ? 'text' : 'password'"
           class="form-input"
           [class.is-invalid]="form.get('adminPassword')?.invalid && form.get('adminPassword')?.touched"
           placeholder="Almeno 8 caratteri complessi"
           formControlName="adminPassword"
         />
         <button type="button" class="password-toggle-btn" (click)="toggleShowPassword()">
           {{ showPassword() ? 'Nascondi' : 'Mostra' }}
         </button>
       </div>
       @if (form.get('adminPassword')?.invalid && form.get('adminPassword')?.touched) {
         @if (form.get('adminPassword')?.hasError('required')) {
           <span class="form-error">La password è obbligatoria.</span>
         } @else if (form.get('adminPassword')?.hasError('strongPassword')) {
           <span class="form-error">La password deve contenere almeno 8 caratteri, una lettera maiuscola, una minuscola, un numero e un carattere speciale.</span>
         }
       }
     </div>
     ```
   - For `confirmPassword`:
     ```html
     <div class="form-group">
       <label class="form-label" for="confirmPassword">Conferma Password</label>
       <div class="password-input-group">
         <input
           id="confirmPassword"
           [type]="showConfirmPassword() ? 'text' : 'password'"
           class="form-input"
           [class.is-invalid]="(form.get('confirmPassword')?.invalid || form.hasError('passwordMismatch')) && form.get('confirmPassword')?.touched"
           placeholder="Ripeti la password"
           formControlName="confirmPassword"
         />
         <button type="button" class="password-toggle-btn" (click)="toggleShowConfirmPassword()">
           {{ showConfirmPassword() ? 'Nascondi' : 'Mostra' }}
         </button>
       </div>
       @if (form.get('confirmPassword')?.touched) {
         @if (form.get('confirmPassword')?.hasError('required')) {
           <span class="form-error">La conferma della password è obbligatoria.</span>
         } @else if (form.hasError('passwordMismatch')) {
           <span class="form-error">Le password non coincidono.</span>
         }
       }
     </div>
     ```
4. **Styles**:
   ```css
   .password-input-group {
     position: relative;
     display: flex;
     align-items: center;
   }
   .password-input-group .form-input {
     padding-right: 5rem;
   }
   .password-toggle-btn {
     position: absolute;
     right: 0.5rem;
     background: none;
     border: none;
     color: var(--color-primary);
     font-size: 0.8125rem;
     font-weight: 500;
     cursor: pointer;
     padding: 0.25rem 0.5rem;
     line-height: 1;
   }
   .password-toggle-btn:hover {
     text-decoration: underline;
   }
   ```
5. **Submission Payload**:
   ```typescript
   const raw = this.form.getRawValue();
   const slug = this.authService.deriveSlug(raw.companyName);

   const payload: RegisterTenantRequest = {
     companyName: raw.companyName,
     slug: slug,
     adminFirstName: 'Admin',
     adminLastName: 'Admin',
     firstName: 'Admin',
     lastName: 'Admin',
     adminEmail: raw.adminEmail,
     adminPassword: raw.adminPassword,
     confirmPassword: raw.confirmPassword
   };
   ```

---

## 5. Verification Method

To verify these changes upon implementation:

1. **Component Test Specs**:
   Create dedicated unit tests:
   - `src/app/features/auth/register-tenant/register-tenant.component.spec.ts`:
     - Test that "Nome" and "Cognome" fields are NOT in the DOM.
     - Test that "Conferma Password" input is present.
     - Test toggle buttons switch input `type` between `password` and `text` and text between `Mostra` and `Nascondi`.
     - Test that `adminPassword` fails validation when missing uppercase, lowercase, digit, or special character.
     - Test that form is invalid and submit disabled when `adminPassword !== confirmPassword`.
     - Test that submitting form sends `adminFirstName: "Admin"`, `adminLastName: "Admin"`, and properly generated `slug`.
   - `src/app/features/auth/login/login.component.spec.ts`:
     - Test that 401 error displays "Credenziali non valide o email non registrata." or ProblemDetails detail in the alert banner.
   - `src/app/core/services/auth.service.spec.ts`:
     - Test `registerTenant` handling response with `accessToken`, `companyName`, and storing them in localStorage and signal state.

2. **Automated Verification Commands**:
   - `npm test -- --watch=false` in `c:\Users\AntonucciMichele\TaskForge\frontend` (must pass 100%).
   - `npm run build` in `c:\Users\AntonucciMichele\TaskForge\frontend` (must compile without errors or bundle warnings).

3. **Invalidation Conditions**:
   - If `npm run build` fails due to missing properties on `RegisterTenantRequest` or `AuthResponse`.
   - If `confirmPassword` toggle button triggers form submit (ensure `type="button"`).
   - If `slug` generated for tenant does not match backend `TenantSlug` regex `^[a-z0-9]+(-[a-z0-9]+)*$`.
