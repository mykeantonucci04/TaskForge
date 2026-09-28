import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ProjectService } from '../../../core/services/project.service';
import { WorkspaceService } from '../../../core/services/workspace.service';
import { BoardSummaryDto } from '../../../core/models/project.models';
import { ModalComponent } from '../../../shared/components/modal/modal.component';

@Component({
  selector: 'app-project-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, ModalComponent],
  template: `
    <div class="project-detail-page">
      <!-- Breadcrumb Navigation -->
      <nav class="breadcrumb">
        <a routerLink="/workspaces" class="breadcrumb-link">Workspaces</a>
        <span class="breadcrumb-separator">/</span>
        @if (workspaceService.selectedWorkspace()) {
          <a [routerLink]="['/workspaces', workspaceService.selectedWorkspace()?.id]" class="breadcrumb-link">
            {{ workspaceService.selectedWorkspace()?.name }}
          </a>
          <span class="breadcrumb-separator">/</span>
        }
        <span class="breadcrumb-current">{{ projectService.currentProject()?.name || 'Progetto' }}</span>
      </nav>

      @if (projectService.isLoading()) {
        <div class="loading-state">
          <div class="spinner"></div>
          <p>Caricamento progetto in corso...</p>
        </div>
      } @else if (projectService.currentProject()) {
        <div class="page-header">
          <div>
            <div class="title-with-key">
              <span class="project-key">{{ projectService.currentProject()?.key }}</span>
              <h1>{{ projectService.currentProject()?.name }}</h1>
            </div>
            <p class="page-subtitle">{{ projectService.currentProject()?.description || 'Nessuna descrizione del progetto.' }}</p>
          </div>
          <button class="btn btn-primary" (click)="openCreateModal()">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="12" y1="5" x2="12" y2="19"></line>
              <line x1="5" y1="12" x2="19" y2="12"></line>
            </svg>
            <span>Nuova Board Kanban</span>
          </button>
        </div>

        <section class="boards-section">
          <h2>Kanban Boards</h2>
          @if (projectService.currentProject()?.boards?.length === 0) {
            <div class="empty-state card">
              <div class="empty-icon">📋</div>
              <h3>Nessuna Board presente</h3>
              <p>Crea la tua prima lavagna Kanban per visualizzare e trascinare i task nelle colonne di lavoro.</p>
              <button class="btn btn-primary" (click)="openCreateModal()">Crea Board Kanban</button>
            </div>
          } @else {
            <div class="board-grid">
              @for (b of projectService.currentProject()?.boards; track b.id) {
                <div class="board-card card" (click)="onSelectBoard(b)">
                  <div class="card-header">
                    <span class="board-icon">📋</span>
                    <h3 class="board-title">{{ b.name }}</h3>
                  </div>
                  <div class="card-footer">
                    <span class="footer-action">Apri Lavagna Kanban</span>
                    <span class="arrow">→</span>
                  </div>
                </div>
              }
            </div>
          }
        </section>
      }

      <!-- Modal Crea Board -->
      <app-modal [isOpen]="isModalOpen()" [title]="'Crea Nuova Board Kanban'" (close)="closeCreateModal()">
        <form [formGroup]="form" (ngSubmit)="onCreateBoard()">
          @if (errorMessage()) {
            <div class="alert alert-danger">{{ errorMessage() }}</div>
          }

          <div class="form-group">
            <label class="form-label" for="board-name">Nome della Board</label>
            <input
              id="board-name"
              type="text"
              class="form-input"
              placeholder="es. Sprint Attivo / Backlog"
              formControlName="name"
            />
            @if (form.get('name')?.invalid && form.get('name')?.touched) {
              <span class="form-error">Il nome della board è obbligatorio.</span>
            }
          </div>

          <div class="checkbox-group">
            <label class="checkbox-label">
              <input type="checkbox" formControlName="createDefaultColumns" />
              <span>Crea colonne predefinite (To Do, In Progress, Done)</span>
            </label>
          </div>

          <div class="modal-actions">
            <button type="button" class="btn btn-secondary" (click)="closeCreateModal()">Annulla</button>
            <button type="submit" class="btn btn-primary" [disabled]="form.invalid || isSubmitting()">
              {{ isSubmitting() ? 'Creazione...' : 'Crea Board' }}
            </button>
          </div>
        </form>
      </app-modal>
    </div>
  `,
  styles: [`
    .project-detail-page {
      max-width: 1100px;
      margin: 0 auto;
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    .breadcrumb {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.8125rem;
      color: var(--text-secondary);
    }

    .breadcrumb-link {
      color: var(--color-primary);
      text-decoration: none;
    }

    .breadcrumb-link:hover {
      text-decoration: underline;
    }

    .page-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding-bottom: 1rem;
      border-bottom: 1px solid var(--border-color);
    }

    .title-with-key {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .project-key {
      padding: 0.2rem 0.55rem;
      background-color: var(--color-primary-light);
      color: var(--color-primary);
      border: 1px solid var(--color-primary-border);
      border-radius: var(--radius-sm);
      font-weight: 700;
      font-size: 0.85rem;
      letter-spacing: 0.05em;
    }

    .page-subtitle {
      color: var(--text-secondary);
      font-size: 0.875rem;
      margin-top: 0.35rem;
    }

    .boards-section {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .boards-section h2 {
      font-size: 1.15rem;
      color: var(--text-main);
    }

    .board-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 1.25rem;
    }

    .board-card {
      padding: 1.25rem;
      cursor: pointer;
      transition: all 0.15s ease-in-out;
      display: flex;
      flex-direction: column;
      justify-content: space-between;
      min-height: 120px;
    }

    .board-card:hover {
      border-color: var(--color-primary);
      box-shadow: var(--shadow-md);
      transform: translateY(-2px);
    }

    .card-header {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .board-icon {
      font-size: 1.35rem;
    }

    .board-title {
      font-size: 1.05rem;
      font-weight: 600;
      color: var(--text-main);
    }

    .card-footer {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding-top: 0.75rem;
      border-top: 1px solid var(--border-color);
      font-size: 0.8125rem;
      color: var(--color-primary);
      font-weight: 500;
    }

    .checkbox-group {
      margin-top: 1rem;
      margin-bottom: 1.5rem;
    }

    .checkbox-label {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.875rem;
      color: var(--text-main);
      cursor: pointer;
    }

    .modal-actions {
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
    }

    .loading-state, .empty-state {
      padding: 3rem 1.5rem;
      text-align: center;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 1rem;
    }

    .empty-icon {
      font-size: 2.5rem;
    }

    .spinner {
      width: 32px;
      height: 32px;
      border: 3px solid var(--border-color);
      border-top-color: var(--color-primary);
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }
  `]
})
export class ProjectDetailComponent implements OnInit {
  readonly projectService = inject(ProjectService);
  readonly workspaceService = inject(WorkspaceService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly projectId = signal<string>('');
  readonly isModalOpen = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    createDefaultColumns: [true]
  });

  ngOnInit(): void {
    const pId = this.route.snapshot.paramMap.get('projectId');
    if (pId) {
      this.projectId.set(pId);
      this.projectService.getProjectById(pId).subscribe();
    }
  }

  openCreateModal(): void {
    this.form.reset({ name: '', createDefaultColumns: true });
    this.errorMessage.set(null);
    this.isModalOpen.set(true);
  }

  closeCreateModal(): void {
    this.isModalOpen.set(false);
  }

  onCreateBoard(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const pId = this.projectId();
    const req = {
      name: this.form.value.name!,
      createDefaultColumns: this.form.value.createDefaultColumns
    };

    this.projectService.createBoard(pId, req).subscribe({
      next: (board) => {
        this.isSubmitting.set(false);
        this.closeCreateModal();
        this.router.navigate(['/boards', board.id]);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        const msg = err.error?.detail || err.error?.message || 'Errore durante la creazione della board.';
        this.errorMessage.set(msg);
      }
    });
  }

  onSelectBoard(board: BoardSummaryDto): void {
    this.router.navigate(['/boards', board.id]);
  }
}
