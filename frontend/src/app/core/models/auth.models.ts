export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterTenantRequest {
  companyName: string;
  slug?: string;
  adminFirstName?: string;
  adminLastName?: string;
  adminEmail: string;
  adminPassword: string;
  confirmPassword?: string;
  firstName?: string;
  lastName?: string;
  plan?: number;
}

export interface AuthResponse {
  accessToken?: string;
  token?: string;
  refreshToken?: string;
  refreshTokenExpiryUtc?: string;
  expiresAt?: string;
  tenantId?: string;
  companyName?: string;
  tenantName?: string;
  slug?: string;
  schemaName?: string;
  role?: string;
  adminUserId?: string;
  adminEmail?: string;
  adminFullName?: string;
  userId?: string;
  email?: string;
  firstName?: string;
  lastName?: string;
  user?: {
    id: string;
    email: string;
    firstName?: string;
    lastName?: string;
    fullName?: string;
    role?: string;
    tenantId?: string;
    schemaName?: string;
  };
}

export interface CurrentUser {
  id: string;
  email: string;
  fullName: string;
  tenantId: string;
  tenantName?: string;
}
