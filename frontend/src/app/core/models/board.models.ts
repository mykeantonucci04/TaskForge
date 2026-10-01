import { TaskDto } from './task.models';

export enum TaskItemStatus {
  Backlog = 1,
  Todo = 2,
  InProgress = 3,
  InReview = 4,
  Done = 5
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

export interface ColumnOrderDto {
  columnId: string;
  order: number;
}

export interface ReorderColumnsRequest {
  columns: ColumnOrderDto[];
}
