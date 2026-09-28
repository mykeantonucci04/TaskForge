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

  describe('deriveSlug', () => {
    it('should generate valid URL-safe slugs conforming to ^[a-z0-9]+(-[a-z0-9]+)*$', () => {
      const slugRegex = /^[a-z0-9]+(-[a-z0-9]+)*$/;

      const slug1 = service.deriveSlug('Acme S.p.A.');
      expect(slug1).toBe('acme-s-p-a');
      expect(slugRegex.test(slug1)).toBe(true);
      expect(slug1.length).toBeGreaterThanOrEqual(3);
      expect(slug1.length).toBeLessThanOrEqual(50);

      const slug2 = service.deriveSlug('Società Elettrica 2026!');
      expect(slug2).toBe('societa-elettrica-2026');
      expect(slugRegex.test(slug2)).toBe(true);

      const slugShort = service.deriveSlug('A');
      expect(slugShort).toBe('a-org');
      expect(slugRegex.test(slugShort)).toBe(true);

      const slugEmpty = service.deriveSlug('');
      expect(slugEmpty).toBe('workspace');
      expect(slugRegex.test(slugEmpty)).toBe(true);
    });
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

  it('should support backend login response format with accessToken and user object', () => {
    const mockResponse = {
      accessToken: 'backend-jwt-access-token',
      refreshToken: 'backend-refresh-token',
      user: {
        id: 'user-guid-001',
        email: 'ceo@enterprise.com',
        fullName: 'Jane Doe',
        tenantId: 'tenant-guid-001',
        schemaName: 'tenant_enterprise'
      }
    };

    service.login({ email: 'ceo@enterprise.com', password: 'Password123!' }).subscribe(res => {
      expect(res.accessToken).toBe('backend-jwt-access-token');
      expect(service.isAuthenticated()).toBe(true);
      expect(service.getToken()).toBe('backend-jwt-access-token');
      expect(service.currentUser()?.id).toBe('user-guid-001');
      expect(service.currentUser()?.fullName).toBe('Jane Doe');
      expect(service.currentUser()?.tenantId).toBe('tenant-guid-001');
      expect(service.currentUser()?.tenantName).toBe('tenant_enterprise');
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    req.flush(mockResponse);
  });

  it('should register tenant, apply default admin names and derived slug, and process response', () => {
    const mockResponse = {
      accessToken: 'tenant-admin-token',
      tenantId: 'tenant-guid-999',
      companyName: 'Acme Global',
      slug: 'acme-global',
      adminUserId: 'admin-guid-999',
      adminEmail: 'admin@acmeglobal.com',
      adminFullName: 'Admin Admin'
    };

    service.registerTenant({
      companyName: 'Acme Global',
      adminEmail: 'admin@acmeglobal.com',
      adminPassword: 'Password123!'
    }).subscribe(res => {
      expect(res.accessToken).toBe('tenant-admin-token');
      expect(service.isAuthenticated()).toBe(true);
      expect(service.getToken()).toBe('tenant-admin-token');
      expect(service.currentUser()?.id).toBe('admin-guid-999');
      expect(service.currentUser()?.email).toBe('admin@acmeglobal.com');
      expect(service.currentUser()?.tenantName).toBe('Acme Global');
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/auth/register-tenant`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      companyName: 'Acme Global',
      adminEmail: 'admin@acmeglobal.com',
      adminPassword: 'Password123!',
      adminFirstName: 'Admin',
      adminLastName: 'Admin',
      slug: 'acme-global'
    });

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
