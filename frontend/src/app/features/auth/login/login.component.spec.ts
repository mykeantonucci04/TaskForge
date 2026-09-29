import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { LoginComponent } from './login.component';
import { AuthService } from '../../../core/services/auth.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authService: AuthService;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'dashboard', component: class DummyDashboardComponent {} }])
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    authService = TestBed.inject(AuthService);
    router = TestBed.inject(Router);
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should have invalid form initially and valid when populated', () => {
    expect(component.form.valid).toBe(false);

    component.form.setValue({
      email: 'user@example.com',
      password: 'Password123!'
    });

    expect(component.form.valid).toBe(true);
  });

  it('should mark all fields as touched if submitting while invalid', () => {
    component.onSubmit();
    expect(component.form.get('email')?.touched).toBe(true);
    expect(component.form.get('password')?.touched).toBe(true);
  });

  it('should display error message in alert box when receiving HTTP 401 with ProblemDetails detail', () => {
    const errorDetail = 'Credenziali non valide o email non registrata.';
    const httpError = new HttpErrorResponse({
      error: { detail: errorDetail },
      status: 401,
      statusText: 'Unauthorized'
    });

    vi.spyOn(authService, 'login').mockReturnValue(throwError(() => httpError));

    component.form.setValue({
      email: 'unregistered@example.com',
      password: 'Password123!'
    });

    component.onSubmit();
    fixture.detectChanges();

    expect(component.errorMessage()).toBe(errorDetail);

    const alertEl = fixture.nativeElement.querySelector('.alert.alert-danger');
    expect(alertEl).toBeTruthy();
    expect(alertEl.textContent).toContain(errorDetail);
  });

  it('should display default 401 friendly error message when error detail is missing', () => {
    const httpError = new HttpErrorResponse({
      error: null,
      status: 401,
      statusText: 'Unauthorized'
    });

    vi.spyOn(authService, 'login').mockReturnValue(throwError(() => httpError));

    component.form.setValue({
      email: 'unregistered@example.com',
      password: 'Password123!'
    });

    component.onSubmit();
    fixture.detectChanges();

    const expectedMsg = 'Credenziali non valide o email non registrata.';
    expect(component.errorMessage()).toBe(expectedMsg);

    const alertEl = fixture.nativeElement.querySelector('.alert.alert-danger');
    expect(alertEl).toBeTruthy();
    expect(alertEl.textContent).toContain(expectedMsg);
  });

  it('should navigate to dashboard on successful login', () => {
    const navigateSpy = vi.spyOn(router, 'navigate');
    vi.spyOn(authService, 'login').mockReturnValue(of({
      accessToken: 'token-abc',
      tenantId: 'tenant-123'
    }));

    component.form.setValue({
      email: 'admin@acme.com',
      password: 'Password123!'
    });

    component.onSubmit();
    fixture.detectChanges();

    expect(navigateSpy).toHaveBeenCalledWith(['/dashboard']);
    expect(component.errorMessage()).toBeNull();
  });
});
