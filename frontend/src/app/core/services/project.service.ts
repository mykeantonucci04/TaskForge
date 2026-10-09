import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ProjectDto, ProjectDetailDto, CreateProjectRequest, UpdateProjectRequest, BoardSummaryDto } from '../models/project.models';
import { BoardDto, CreateBoardRequest } from '../models/board.models';

@Injectable({
  providedIn: 'root'
})
export class ProjectService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = environment.apiUrl;

  readonly projects = signal<ProjectDto[]>([]);
  readonly currentProject = signal<ProjectDetailDto | null>(null);
  readonly isLoading = signal<boolean>(false);

  loadProjectsByWorkspace(workspaceId: string): Observable<ProjectDto[]> {
    this.isLoading.set(true);
    return this.http.get<ProjectDto[]>(`${this.apiBase}/workspaces/${workspaceId}/projects`).pipe(
      tap({
        next: (list) => {
          this.projects.set(list);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      })
    );
  }

  createProject(workspaceId: string, request: CreateProjectRequest): Observable<ProjectDto> {
    return this.http.post<ProjectDto>(`${this.apiBase}/workspaces/${workspaceId}/projects`, request).pipe(
      tap((newPrj) => {
        this.projects.update(list => [...list, newPrj]);
      })
    );
  }

  getProjectById(projectId: string): Observable<ProjectDetailDto> {
    this.isLoading.set(true);
    return this.http.get<ProjectDetailDto>(`${this.apiBase}/projects/${projectId}`).pipe(
      tap({
        next: (detail) => {
          this.currentProject.set(detail);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      })
    );
  }

  updateProject(id: string, request: UpdateProjectRequest): Observable<ProjectDto> {
    return this.http.put<ProjectDto>(`${this.apiBase}/projects/${id}`, request).pipe(
      tap((updated) => {
        this.projects.update(list => list.map(p => p.id === id ? { ...p, ...updated } : p));
        const current = this.currentProject();
        if (current && current.id === id) {
          this.currentProject.set({
            ...current,
            name: updated.name,
            description: updated.description
          });
        }
      })
    );
  }

  deleteProject(id: string): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiBase}/projects/${id}`).pipe(
      tap(() => {
        this.projects.update(list => list.filter(p => p.id !== id));
        if (this.currentProject()?.id === id) {
          this.currentProject.set(null);
        }
      })
    );
  }

  getBoardsByProject(projectId: string): Observable<BoardSummaryDto[]> {
    return this.http.get<BoardSummaryDto[]>(`${this.apiBase}/projects/${projectId}/boards`);
  }

  createBoard(projectId: string, request: CreateBoardRequest): Observable<BoardDto> {
    return this.http.post<BoardDto>(`${this.apiBase}/projects/${projectId}/boards`, request).pipe(
      tap((newBoard) => {
        const current = this.currentProject();
        if (current && current.id === projectId) {
          const summary: BoardSummaryDto = {
            id: newBoard.id,
            name: newBoard.name,
            createdAtUtc: newBoard.createdAtUtc
          };
          this.currentProject.set({
            ...current,
            boards: [...current.boards, summary]
          });
        }
      })
    );
  }
}
