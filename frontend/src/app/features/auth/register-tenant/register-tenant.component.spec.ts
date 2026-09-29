import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { RegisterTenantComponent, strongPasswordValidator } from './register-tenant.component';
import { AuthService } from '../../../core/services/auth.service';
import { FormControl } from '@angular/forms';

describe('RegisterTenantComponent', () => {
  let component: RegisterTenantComponent;
  let fixture: ComponentFixture<RegisterTenantComponent>;
  let authService: AuthService;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegisterTenantComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'dashboard', component: class DummyDashboardComponent {} }])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(RegisterTenantComponent);
    component = fixture.componentInstance;
    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('R3: should NOT contain firstName or lastName input fields in template', () => {
    const el = fixture.nativeElement as HTMLElement;
    const firstNameInput = el.querySelector('#firstName');
    const lastNameInput = el.querySelector('#lastName');
    const firstNameControl = component.form.get('firstName');
    const lastNameControl = component.form.get('lastName');

    expect(firstNameInput).toBeNull();
    expect(lastNameInput).toBeNull();
    expect(firstNameControl).toBeNull();
    expect(lastNameControl).toBeNull();
  });

  it('R1: should contain Conferma Password field and label', () => {
    const el = fixture.nativeElement as HTMLElement;
    const confirmInput = el.querySelector('#confirmPassword') as HTMLInputElement;
    const label = el.querySelector('label[for="confirmPassword"]');

    expect(confirmInput).toBeTruthy();
    expect(label?.textContent).toContain('Conferma Password');
    expect(component.form.get('confirmPassword')).toBeTruthy();
  });

  it('R1: should toggle adminPassword visibility between password and text with button', () => {
    const el = fixture.nativeElement as HTMLElement;
    const passwordInput = el.querySelector('#adminPassword') as HTMLInputElement;
    const toggleButtons = el.querySelectorAll('.password-toggle-btn');
    const passwordToggleBtn = toggleButtons[0] as HTMLButtonElement;

    expect(passwordToggleBtn.getAttribute('type')).toBe('button');
    expect(passwordInput.type).toBe('password');
    expect(passwordToggleBtn.textContent?.trim()).toBe('Mostra');

    passwordToggleBtn.click();
    fixture.detectChanges();

    expect(passwordInput.type).toBe('text');
    expect(passwordToggleBtn.textContent?.trim()).toBe('Nascondi');

    passwordToggleBtn.click();
    fixture.detectChanges();

    expect(passwordInput.type).toBe('password');
    expect(passwordToggleBtn.textContent?.trim()).toBe('Mostra');
  });

  it('R1: should toggle confirmPassword visibility between password and text with button', () => {
    const el = fixture.nativeElement as HTMLElement;
    const confirmInput = el.querySelector('#confirmPassword') as HTMLInputElement;
    const toggleButtons = el.querySelectorAll('.password-toggle-btn');
    const confirmToggleBtn = toggleButtons[1] as HTMLButtonElement;

    expect(confirmToggleBtn.getAttribute('type')).toBe('button');
    expect(confirmInput.type).toBe('password');
    expect(confirmToggleBtn.textContent?.trim()).toBe('Mostra');

    confirmToggleBtn.click();
    fixture.detectChanges();

    expect(confirmInput.type).toBe('text');
    expect(confirmToggleBtn.textContent?.trim()).toBe('Nascondi');

    confirmToggleBtn.click();
    fixture.detectChanges();

    expect(confirmInput.type).toBe('password');
    expect(confirmToggleBtn.textContent?.trim()).toBe('Mostra');
  });

  describe('R4: Strong Password Validation', () => {
    it('should validate password complexity requirements', () => {
      // Test length < 8
      expect(strongPasswordValidator(new FormControl('Aa1!bcd'))).toEqual({ strongPassword: true });
      // Test missing uppercase
      expect(strongPasswordValidator(new FormControl('password123!'))).toEqual({ strongPassword: true });
      // Test missing lowercase
      expect(strongPasswordValidator(new FormControl('PASSWORD123!'))).toEqual({ strongPassword: true });
      // Test missing digit
      expect(strongPasswordValidator(new FormControl('Password!@#'))).toEqual({ strongPassword: true });
      // Test missing special character
      expect(strongPasswordValidator(new FormControl('Password123'))).toEqual({ strongPassword: true });
      // Valid password
      expect(strongPasswordValidator(new FormControl('Password123!'))).toBeNull();
      // Empty input returns null (handled by Validators.required)
      expect(strongPasswordValidator(new FormControl(''))).toBeNull();
    });

    it('should show error in template when password is weak and touched', () => {
      const passwordControl = component.form.get('adminPassword');
      passwordControl?.setValue('weak');
      passwordControl?.markAsTouched();
      fixture.detectChanges();

      const errorSpan = fixture.nativeElement.querySelector('.form-group .form-error');
      expect(errorSpan).toBeTruthy();
      expect(errorSpan.textContent).toContain('La password deve contenere almeno 8 caratteri');
    });
  });

  describe('R1: Password Confirmation Match', () => {
    it('should invalidate form and show error when passwords do not match', () => {
      component.form.patchValue({
        companyName: 'Acme Corp',
        adminEmail: 'admin@acme.com',
        adminPassword: 'Password123!',
        confirmPassword: 'DifferentPassword123!'
      });

      expect(component.form.hasError('passwordMismatch')).toBe(true);
      expect(component.form.valid).toBe(false);

      component.form.get('confirmPassword')?.markAsTouched();
      fixture.detectChanges();

      const errorSpan = fixture.nativeElement.querySelector('.form-group:nth-of-type(4) .form-error');
      expect(errorSpan).toBeTruthy();
      expect(errorSpan.textContent).toContain('Le password non coincidono');
    });

    it('should validate successfully when passwords match', () => {
      component.form.patchValue({
        companyName: 'Acme Corp',
        adminEmail: 'admin@acme.com',
        adminPassword: 'Password123!',
        confirmPassword: 'Password123!'
      });

      expect(component.form.hasError('passwordMismatch')).toBe(false);
      expect(component.form.valid).toBe(true);
    });
  });

  describe('R3 & R5: Submission Payload & Slug Derivation', () => {
    it('should automatically derive slug and supply adminFirstName and adminLastName as Admin', () => {
      const registerSpy = vi.spyOn(authService, 'registerTenant').mockReturnValue(of({
        accessToken: 'mock-access-token',
        tenantId: 'tenant-guid-123',
        companyName: 'Acme Industries S.p.A.'
      }));
      const navigateSpy = vi.spyOn(router, 'navigate');

      component.form.setValue({
        companyName: 'Acme Industries S.p.A.',
        adminEmail: 'admin@acme.com',
        adminPassword: 'Password123!',
        confirmPassword: 'Password123!'
      });

      component.onSubmit();
      fixture.detectChanges();

      expect(registerSpy).toHaveBeenCalledTimes(1);
      const payload = registerSpy.mock.calls[0][0];

      expect(payload.companyName).toBe('Acme Industries S.p.A.');
      expect(payload.slug).toBe('acme-industries-s-p-a');
      expect(payload.adminFirstName).toBe('Admin');
      expect(payload.adminLastName).toBe('Admin');
      expect(payload.adminEmail).toBe('admin@acme.com');
      expect(payload.adminPassword).toBe('Password123!');
      expect(payload.confirmPassword).toBe('Password123!');
      expect(navigateSpy).toHaveBeenCalledWith(['/dashboard']);
    });

    it('should display error message when registration fails', () => {
      const errorMessage = 'Tenant già registrato con questa email.';
      vi.spyOn(authService, 'registerTenant').mockReturnValue(throwError(() => ({
        error: { detail: errorMessage }
      })));

      component.form.setValue({
        companyName: 'Acme Corp',
        adminEmail: 'admin@acme.com',
        adminPassword: 'Password123!',
        confirmPassword: 'Password123!'
      });

      component.onSubmit();
      fixture.detectChanges();

      expect(component.errorMessage()).toBe(errorMessage);
      const alertEl = fixture.nativeElement.querySelector('.alert.alert-danger');
      expect(alertEl).toBeTruthy();
      expect(alertEl.textContent).toContain(errorMessage);
    });
  });
});
