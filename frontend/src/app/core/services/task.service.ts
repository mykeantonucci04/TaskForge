import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  TaskDto,
  TaskDetailDto,
  CreateTaskRequest,
  UpdateTaskRequest,
  MoveTaskRequest,
  SubtaskDto,
  AddSubtaskRequest,
  CommentDto,
  AddCommentRequest,
  LabelDto,
  CreateLabelRequest
} from '../models/task.models';

@Injectable({
  providedIn: 'root'
})
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = environment.apiUrl;

  createTask(request: CreateTaskRequest): Observable<TaskDto> {
    return this.http.post<TaskDto>(`${this.apiBase}/tasks`, request);
  }

  getTaskById(taskId: string): Observable<TaskDetailDto> {
    return this.http.get<TaskDetailDto>(`${this.apiBase}/tasks/${taskId}`);
  }

  updateTask(taskId: string, request: UpdateTaskRequest): Observable<TaskDto> {
    return this.http.put<TaskDto>(`${this.apiBase}/tasks/${taskId}`, request);
  }

  deleteTask(taskId: string): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiBase}/tasks/${taskId}`);
  }

  moveTask(taskId: string, request: MoveTaskRequest): Observable<TaskDto> {
    return this.http.post<TaskDto>(`${this.apiBase}/tasks/${taskId}/move`, request);
  }

  // Subtasks
  addSubtask(taskId: string, request: AddSubtaskRequest): Observable<SubtaskDto> {
    return this.http.post<SubtaskDto>(`${this.apiBase}/tasks/${taskId}/subtasks`, request);
  }

  toggleSubtask(subtaskId: string): Observable<SubtaskDto> {
    return this.http.patch<SubtaskDto>(`${this.apiBase}/subtasks/${subtaskId}/toggle`, {});
  }

  deleteSubtask(subtaskId: string): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiBase}/subtasks/${subtaskId}`);
  }

  // Comments
  addComment(taskId: string, request: AddCommentRequest): Observable<CommentDto> {
    return this.http.post<CommentDto>(`${this.apiBase}/tasks/${taskId}/comments`, request);
  }

  deleteComment(commentId: string): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiBase}/comments/${commentId}`);
  }

  // Labels
  getWorkspaceLabels(workspaceId: string): Observable<LabelDto[]> {
    return this.http.get<LabelDto[]>(`${this.apiBase}/workspaces/${workspaceId}/labels`);
  }

  createWorkspaceLabel(workspaceId: string, request: CreateLabelRequest): Observable<LabelDto> {
    return this.http.post<LabelDto>(`${this.apiBase}/workspaces/${workspaceId}/labels`, request);
  }

  assignLabelToTask(taskId: string, labelId: string): Observable<boolean> {
    return this.http.post<boolean>(`${this.apiBase}/tasks/${taskId}/labels/${labelId}`, {});
  }

  removeLabelFromTask(taskId: string, labelId: string): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiBase}/tasks/${taskId}/labels/${labelId}`);
  }
}
