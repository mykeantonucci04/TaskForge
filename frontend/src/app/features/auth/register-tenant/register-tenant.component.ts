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
  templateUrl: './register-tenant.component.html',
  styleUrl: './register-tenant.component.css'
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
