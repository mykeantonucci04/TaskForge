export interface ProjectDto {
  id: string;
  tenantId: string;
  workspaceId: string;
  name: string;
  key: string;
  description?: string | null;
  createdAtUtc: string;
}

export interface BoardSummaryDto {
  id: string;
  name: string;
  createdAtUtc: string;
}

export interface ProjectDetailDto {
  id: string;
  tenantId: string;
  workspaceId: string;
  name: string;
  key: string;
  description?: string | null;
  createdAtUtc: string;
  boards: BoardSummaryDto[];
}

export interface CreateProjectRequest {
  name: string;
  key: string;
  description?: string;
}

export interface UpdateProjectRequest {
  name: string;
  description?: string;
}
