import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'auth/login', component: class DummyComponent {} }])
      ]
    });

    service = TestBed.inject(AuthService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
    localStorage.clear();
  });

  it('should be created and initial state unauthenticated', () => {
    expect(service).toBeTruthy();
    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
  });

  it('should authenticate user and store token on login', () => {
    const mockResponse = {
      token: 'jwt-test-token',
      userId: '11111111-1111-1111-1111-111111111111',
      email: 'admin@acme.com',
      firstName: 'Admin',
      lastName: 'User',
      tenantId: '22222222-2222-2222-2222-222222222222',
      tenantName: 'Acme Corp'
    };

    service.login({ email: 'admin@acme.com', password: 'Password123!' }).subscribe(res => {
      expect(res.token).toBe('jwt-test-token');
      expect(service.isAuthenticated()).toBe(true);
      expect(service.currentUser()?.email).toBe('admin@acme.com');
      expect(service.getToken()).toBe('jwt-test-token');
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
  });

  it('should clear user state and storage on logout', () => {
    localStorage.setItem('taskforge_token', 'test-token');
    localStorage.setItem('taskforge_user', JSON.stringify({
      id: '1', email: 'test@acme.com', fullName: 'Test', tenantId: '123'
    }));

    // Recreate to load from storage
    const authSvc = TestBed.inject(AuthService);
    authSvc.logout();

    expect(authSvc.isAuthenticated()).toBe(false);
    expect(authSvc.currentUser()).toBeNull();
    expect(authSvc.getToken()).toBeNull();
  });
});
