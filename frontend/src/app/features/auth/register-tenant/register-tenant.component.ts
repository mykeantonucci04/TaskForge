import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { RegisterTenantRequest } from '../../../core/models/auth.models';

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

@Component({
  selector: 'app-register-tenant',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  template: `
    <div class="auth-wrapper">
      <div class="auth-card card">
        <div class="auth-header">
          <div class="brand-logo">
            <span class="logo-icon">TF</span>
            <span class="logo-title">TaskForge</span>
          </div>
          <h2>Registra Nuova Organizzazione</h2>
          <p>Crea uno spazio di lavoro isolato per il tuo team aziendale.</p>
        </div>

        @if (errorMessage()) {
          <div class="alert alert-danger" role="alert">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="10"></circle>
              <line x1="12" y1="8" x2="12" y2="12"></line>
              <line x1="12" y1="16" x2="12.01" y2="16"></line>
            </svg>
            <span>{{ errorMessage() }}</span>
          </div>
        }

        <form [formGroup]="form" (ngSubmit)="onSubmit()">
          <div class="form-group">
            <label class="form-label" for="companyName">Nome Organizzazione / Azienda</label>
            <input
              id="companyName"
              type="text"
              class="form-input"
              [class.is-invalid]="form.get('companyName')?.invalid && form.get('companyName')?.touched"
              placeholder="Acme SpA"
              formControlName="companyName"
            />
            @if (form.get('companyName')?.invalid && form.get('companyName')?.touched) {
              <span class="form-error">Il nome dell'azienda è obbligatorio.</span>
            }
          </div>

          <div class="form-group">
            <label class="form-label" for="adminEmail">Email Amministratore</label>
            <input
              id="adminEmail"
              type="email"
              class="form-input"
              [class.is-invalid]="form.get('adminEmail')?.invalid && form.get('adminEmail')?.touched"
              placeholder="admin@acme.it"
              formControlName="adminEmail"
            />
            @if (form.get('adminEmail')?.invalid && form.get('adminEmail')?.touched) {
              <span class="form-error">Inserisci un'email valida.</span>
            }
          </div>

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
              <button
                type="button"
                class="password-toggle-btn"
                (click)="toggleShowPassword()"
              >
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
              <button
                type="button"
                class="password-toggle-btn"
                (click)="toggleShowConfirmPassword()"
              >
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

          <button
            type="submit"
            class="btn btn-primary btn-block"
            [disabled]="form.invalid || isLoading()"
          >
            @if (isLoading()) {
              <span>Configurazione Workspace in corso...</span>
            } @else {
              <span>Crea Organizzazione & Inizia</span>
            }
          </button>
        </form>

        <div class="auth-footer">
          <p>
            Hai già un account?
            <a routerLink="/auth/login" class="auth-link">Accedi</a>
          </p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-wrapper {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 1.5rem;
      background-color: var(--bg-app);
    }

    .auth-card {
      width: 100%;
      max-width: 480px;
      padding: 2.25rem;
      background-color: var(--bg-surface);
    }

    .auth-header {
      text-align: center;
      margin-bottom: 1.75rem;
    }

    .brand-logo {
      display: inline-flex;
      align-items: center;
      gap: 0.5rem;
      margin-bottom: 1rem;
    }

    .logo-icon {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 32px;
      height: 32px;
      background-color: var(--color-primary);
      color: #ffffff;
      font-weight: 700;
      font-size: 0.875rem;
      border-radius: var(--radius-md);
    }

    .logo-title {
      font-size: 1.25rem;
      font-weight: 700;
      color: var(--text-main);
      letter-spacing: -0.02em;
    }

    .auth-header h2 {
      font-size: 1.25rem;
      margin-bottom: 0.35rem;
    }

    .auth-header p {
      font-size: 0.875rem;
      color: var(--text-secondary);
    }

    .password-input-group {
      position: relative;
      display: flex;
      align-items: center;
    }

    .password-input-group .form-input {
      padding-right: 5.5rem;
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

    .auth-footer {
      margin-top: 1.5rem;
      padding-top: 1.25rem;
      border-top: 1px solid var(--border-color);
      text-align: center;
      font-size: 0.8125rem;
      color: var(--text-secondary);
    }

    .auth-link {
      color: var(--color-primary);
      font-weight: 500;
      text-decoration: none;
    }

    .auth-link:hover {
      text-decoration: underline;
    }
  `]
})
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

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

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

    this.authService.registerTenant(payload).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading.set(false);
        const status = err.status;
        const body = err.error;

        // Extract error message from ProblemDetails or generic response
        let msg: string;
        if (body?.detail) {
          msg = body.detail;
        } else if (body?.title && body.title !== 'Internal Server Error') {
          msg = body.title;
        } else if (body?.errors && typeof body.errors === 'object') {
          const errorMessages = Object.values(body.errors).flat();
          msg = errorMessages.join('; ');
        } else if (body?.message) {
          msg = body.message;
        } else {
          // Fallback based on HTTP status code
          switch (status) {
            case 400:
              msg = 'Dati non validi. Verifica i campi e riprova.';
              break;
            case 409:
              msg = 'Un\'organizzazione con questo nome o un account con questa email esiste già.';
              break;
            case 422:
              msg = 'Errore durante l\'elaborazione dei dati. Verifica l\'input e riprova.';
              break;
            case 503:
              msg = 'Servizio temporaneamente non disponibile. Riprova tra qualche istante.';
              break;
            case 0:
              msg = 'Impossibile raggiungere il server. Verifica la connessione di rete.';
              break;
            default:
              msg = 'Si è verificato un errore durante la registrazione. Riprova più tardi.';
          }
        }
        this.errorMessage.set(msg);
      }

    });
  }
}
