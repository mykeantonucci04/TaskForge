import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, CurrentUser, LoginRequest, RegisterTenantRequest } from '../models/auth.models';

const TOKEN_KEY = 'taskforge_token';
const USER_KEY = 'taskforge_user';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly baseUrl = `${environment.apiUrl}/auth`;

  // Angular Signals for state management
  readonly currentUser = signal<CurrentUser | null>(this.loadUserFromStorage());
  readonly isAuthenticated = computed(() => !!this.currentUser());
  readonly currentTenantId = computed(() => this.currentUser()?.tenantId ?? null);

  constructor() {}

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, credentials).pipe(
      tap(response => this.handleAuthSuccess(response))
    );
  }

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

    if (!slug) {
      slug = 'workspace';
    }
    if (slug.length < 3) {
      slug = `${slug}-org`;
    }
    if (slug.length > 50) {
      slug = slug.substring(0, 50).replace(/-+$/, '');
      if (slug.length < 3) {
        slug = `${slug}-org`.slice(0, 50);
      }
    }
    return slug;
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.currentUser.set(null);
    this.router.navigate(['/auth/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  private handleAuthSuccess(res: AuthResponse): void {
    const token = res.accessToken || res.token || '';
    const userId = res.user?.id || res.adminUserId || res.userId || '';
    const email = res.user?.email || res.adminEmail || res.email || '';
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

  private loadUserFromStorage(): CurrentUser | null {
    try {
      const stored = localStorage.getItem(USER_KEY);
      return stored ? JSON.parse(stored) : null;
    } catch {
      return null;
    }
  }
}
