import { TaskDto } from './task.models';

export enum TaskItemStatus {
  Todo = 0,
  InProgress = 1,
  InReview = 2,
  Done = 3,
  Blocked = 4
}

export interface BoardDto {
  id: string;
  tenantId: string;
  projectId: string;
  name: string;
  createdAtUtc: string;
}

export interface BoardColumnDto {
  id: string;
  boardId: string;
  name: string;
  order: number;
  targetStatus: TaskItemStatus;
}

export interface BoardColumnDetailDto {
  id: string;
  boardId: string;
  name: string;
  order: number;
  targetStatus: TaskItemStatus;
  tasks: TaskDto[];
}

export interface BoardDetailDto {
  id: string;
  tenantId: string;
  projectId: string;
  name: string;
  createdAtUtc: string;
  columns: BoardColumnDetailDto[];
}

export interface CreateBoardRequest {
  name: string;
  createDefaultColumns?: boolean;
}

export interface CreateBoardColumnRequest {
  name: string;
  targetStatus?: TaskItemStatus;
}
