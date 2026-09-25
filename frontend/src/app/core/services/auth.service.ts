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
    return this.http.post<AuthResponse>(`${this.baseUrl}/register-tenant`, request).pipe(
      tap(response => this.handleAuthSuccess(response))
    );
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
    const token = res.token;
    const userId = res.userId || res.adminUserId || '';
    const email = res.email || res.adminEmail || '';
    const fullName = res.firstName && res.lastName
      ? `${res.firstName} ${res.lastName}`
      : (res.tenantName ? `Admin (${res.tenantName})` : email);
    const tenantId = res.tenantId;
    const tenantName = res.tenantName || 'Workspace';

    const user: CurrentUser = {
      id: userId,
      email,
      fullName,
      tenantId,
      tenantName
    };

    localStorage.setItem(TOKEN_KEY, token);
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
