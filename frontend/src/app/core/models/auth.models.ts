export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterTenantRequest {
  companyName: string;
  adminEmail: string;
  adminPassword: string;
  firstName: string;
  lastName: string;
}

export interface AuthResponse {
  token: string;
  refreshToken?: string;
  expiresAt?: string;
  userId?: string;
  adminUserId?: string;
  email?: string;
  adminEmail?: string;
  firstName?: string;
  lastName?: string;
  tenantId: string;
  tenantName?: string;
}

export interface CurrentUser {
  id: string;
  email: string;
  fullName: string;
  tenantId: string;
  tenantName?: string;
}
