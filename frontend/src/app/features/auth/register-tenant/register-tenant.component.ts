import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

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

          <div class="form-row">
            <div class="form-group">
              <label class="form-label" for="firstName">Nome</label>
              <input
                id="firstName"
                type="text"
                class="form-input"
                [class.is-invalid]="form.get('firstName')?.invalid && form.get('firstName')?.touched"
                placeholder="Mario"
                formControlName="firstName"
              />
              @if (form.get('firstName')?.invalid && form.get('firstName')?.touched) {
                <span class="form-error">Campo obbligatorio.</span>
              }
            </div>

            <div class="form-group">
              <label class="form-label" for="lastName">Cognome</label>
              <input
                id="lastName"
                type="text"
                class="form-input"
                [class.is-invalid]="form.get('lastName')?.invalid && form.get('lastName')?.touched"
                placeholder="Rossi"
                formControlName="lastName"
              />
              @if (form.get('lastName')?.invalid && form.get('lastName')?.touched) {
                <span class="form-error">Campo obbligatorio.</span>
              }
            </div>
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
            <input
              id="adminPassword"
              type="password"
              class="form-input"
              [class.is-invalid]="form.get('adminPassword')?.invalid && form.get('adminPassword')?.touched"
              placeholder="Almeno 8 caratteri"
              formControlName="adminPassword"
            />
            @if (form.get('adminPassword')?.invalid && form.get('adminPassword')?.touched) {
              <span class="form-error">La password deve contenere almeno 8 caratteri.</span>
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

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 0.75rem;
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

  readonly form = this.fb.nonNullable.group({
    companyName: ['', [Validators.required, Validators.minLength(2)]],
    firstName: ['', [Validators.required]],
    lastName: ['', [Validators.required]],
    adminEmail: ['', [Validators.required, Validators.email]],
    adminPassword: ['', [Validators.required, Validators.minLength(8)]]
  });

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.registerTenant(this.form.getRawValue()).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading.set(false);
        const msg = err.error?.detail || err.error?.message || 'Errore durante la registrazione del tenant.';
        this.errorMessage.set(msg);
      }
    });
  }
}
