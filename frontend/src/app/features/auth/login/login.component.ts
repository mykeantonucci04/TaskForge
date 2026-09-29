import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
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
          <h2>Accedi alla piattaforma</h2>
          <p>Inserisci le tue credenziali aziendali per accedere ai tuoi progetti.</p>
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
            <label class="form-label" for="email">Email</label>
            <input
              id="email"
              type="email"
              class="form-input"
              [class.is-invalid]="form.get('email')?.invalid && form.get('email')?.touched"
              placeholder="mario.rossi@azienda.it"
              formControlName="email"
            />
            @if (form.get('email')?.invalid && form.get('email')?.touched) {
              <span class="form-error">Inserisci un indirizzo email valido.</span>
            }
          </div>

          <div class="form-group">
            <label class="form-label" for="password">Password</label>
            <div class="password-input-group">
              <input
                id="password"
                [type]="showPassword() ? 'text' : 'password'"
                class="form-input"
                [class.is-invalid]="form.get('password')?.invalid && form.get('password')?.touched"
                placeholder="••••••••"
                formControlName="password"
              />
              <button
                type="button"
                class="password-toggle-btn"
                (click)="toggleShowPassword()"
              >
                {{ showPassword() ? 'Nascondi' : 'Mostra' }}
              </button>
            </div>
            @if (form.get('password')?.invalid && form.get('password')?.touched) {
              <span class="form-error">La password è obbligatoria.</span>
            }
          </div>

          <button
            type="submit"
            class="btn btn-primary btn-block"
            [disabled]="form.invalid || isLoading()"
          >
            @if (isLoading()) {
              <span>Accesso in corso...</span>
            } @else {
              <span>Accedi</span>
            }
          </button>
        </form>

        <div class="auth-footer">
          <p>
            La tua organizzazione non è ancora registrata?
            <a routerLink="/auth/register-tenant" class="auth-link">Registra Tenant</a>
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
      max-width: 420px;
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
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly showPassword = signal(false);

  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  toggleShowPassword(): void {
    this.showPassword.update(v => !v);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.authService.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.isLoading.set(false);
        let msg = 'Credenziali non valide o errore di connessione.';
        if (err.status === 401) {
          msg = err.error?.detail || err.error?.message || 'Credenziali non valide o email non registrata.';
        } else if (err.error?.detail) {
          msg = err.error.detail;
        } else if (err.error?.message) {
          msg = err.error.message;
        }
        this.errorMessage.set(msg);
      }
    });
  }
}
