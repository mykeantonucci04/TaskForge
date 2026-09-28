export interface WorkspaceDto {
  id: string;
  tenantId: string;
  name: string;
  slug: string;
  description?: string | null;
  createdAtUtc: string;
}

export interface CreateWorkspaceRequest {
  name: string;
  slug: string;
  description?: string;
}
