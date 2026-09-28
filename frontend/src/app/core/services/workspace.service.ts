import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { WorkspaceDto, CreateWorkspaceRequest } from '../models/workspace.models';

const SELECTED_WORKSPACE_KEY = 'taskforge_selected_workspace';

@Injectable({
  providedIn: 'root'
})
export class WorkspaceService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/workspaces`;

  readonly workspaces = signal<WorkspaceDto[]>([]);
  readonly selectedWorkspace = signal<WorkspaceDto | null>(this.loadSelectedFromStorage());
  readonly isLoading = signal<boolean>(false);

  loadWorkspaces(): Observable<WorkspaceDto[]> {
    this.isLoading.set(true);
    return this.http.get<WorkspaceDto[]>(this.baseUrl).pipe(
      tap({
        next: (list) => {
          this.workspaces.set(list);
          this.isLoading.set(false);
          // Auto-select first workspace if none selected or if selected is not in list
          const current = this.selectedWorkspace();
          if (list.length > 0 && (!current || !list.some(w => w.id === current.id))) {
            this.selectWorkspace(list[0]);
          }
        },
        error: () => this.isLoading.set(false)
      })
    );
  }

  createWorkspace(request: CreateWorkspaceRequest): Observable<WorkspaceDto> {
    return this.http.post<WorkspaceDto>(this.baseUrl, request).pipe(
      tap((newWorkspace) => {
        this.workspaces.update(list => [...list, newWorkspace]);
        this.selectWorkspace(newWorkspace);
      })
    );
  }

  selectWorkspace(workspace: WorkspaceDto): void {
    this.selectedWorkspace.set(workspace);
    try {
      localStorage.setItem(SELECTED_WORKSPACE_KEY, JSON.stringify(workspace));
    } catch {}
  }

  private loadSelectedFromStorage(): WorkspaceDto | null {
    try {
      const stored = localStorage.getItem(SELECTED_WORKSPACE_KEY);
      return stored ? JSON.parse(stored) : null;
    } catch {
      return null;
    }
  }
}
