import { TaskItemStatus } from './board.models';

export enum TaskPriority {
  Low = 1,
  Medium = 2,
  High = 3,
  Urgent = 4
}

export interface SubtaskDto {
  id: string;
  taskItemId: string;
  title: string;
  isCompleted: boolean;
}

export interface CommentDto {
  id: string;
  taskItemId: string;
  authorId: string;
  content: string;
  createdAtUtc: string;
}

export interface LabelDto {
  id: string;
  workspaceId: string;
  name: string;
  colorHex: string;
}

export interface TaskDto {
  id: string;
  columnId: string;
  assigneeId?: string | null;
  title: string;
  description?: string | null;
  priority: TaskPriority;
  status: TaskItemStatus;
  position: number;
  dueDateUtc?: string | null;
  createdAtUtc: string;
}

export interface TaskDetailDto extends TaskDto {
  subtasks: SubtaskDto[];
  comments: CommentDto[];
  labels: LabelDto[];
}

export interface CreateTaskRequest {
  columnId: string;
  title: string;
  description?: string;
  priority?: TaskPriority;
  dueDateUtc?: string;
}

export interface UpdateTaskRequest {
  title: string;
  description?: string;
  priority: TaskPriority;
  assigneeId?: string;
  dueDateUtc?: string;
}

export interface MoveTaskRequest {
  targetColumnId: string;
  newPosition: number;
  newStatus?: TaskItemStatus;
}

export interface AddSubtaskRequest {
  title: string;
}

export interface AddCommentRequest {
  content: string;
}

export interface CreateLabelRequest {
  name: string;
  colorHex: string;
}
