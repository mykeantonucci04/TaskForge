import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { WorkspaceService } from '../../../core/services/workspace.service';
import { ProjectService } from '../../../core/services/project.service';
import { ProjectDto } from '../../../core/models/project.models';
import { ModalComponent } from '../../../shared/components/modal/modal.component';

@Component({
  selector: 'app-workspace-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, ModalComponent],
  template: `
    <div class="workspace-detail-page">
      <!-- Breadcrumb Navigation -->
      <nav class="breadcrumb">
        <a routerLink="/workspaces" class="breadcrumb-link">Workspaces</a>
        <span class="breadcrumb-separator">/</span>
        <span class="breadcrumb-current">{{ currentWorkspaceName() }}</span>
      </nav>

      <div class="page-header">
        <div>
          <h1>{{ currentWorkspaceName() }}</h1>
          <p class="page-subtitle">Progetti attivi all'interno di questo spazio di lavoro.</p>
        </div>
        <button class="btn btn-primary" (click)="openCreateModal()">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19"></line>
            <line x1="5" y1="12" x2="19" y2="12"></line>
          </svg>
          <span>Nuovo Progetto</span>
        </button>
      </div>

      @if (projectService.isLoading()) {
        <div class="loading-state">
          <div class="spinner"></div>
          <p>Caricamento progetti in corso...</p>
        </div>
      } @else if (projectService.projects().length === 0) {
        <div class="empty-state card">
          <div class="empty-icon">📊</div>
          <h3>Nessun progetto trovato</h3>
          <p>Crea il primo progetto per organizzare le tue board Kanban e i relativi task.</p>
          <button class="btn btn-primary" (click)="openCreateModal()">Crea Progetto</button>
        </div>
      } @else {
        <div class="project-grid">
          @for (prj of projectService.projects(); track prj.id) {
            <div class="project-card card" (click)="onSelectProject(prj)">
              <div class="card-header">
                <span class="project-key">{{ prj.key }}</span>
                <h3 class="project-title">{{ prj.name }}</h3>
              </div>
              <p class="project-description">{{ prj.description || 'Nessuna descrizione del progetto.' }}</p>
              <div class="card-footer">
                <span class="footer-text">Visualizza Board Kanban</span>
                <span class="arrow">→</span>
              </div>
            </div>
          }
        </div>
      }

      <!-- Modal Crea Progetto -->
      <app-modal [isOpen]="isModalOpen()" [title]="'Crea Nuovo Progetto'" (close)="closeCreateModal()">
        <form [formGroup]="form" (ngSubmit)="onCreateProject()">
          @if (errorMessage()) {
            <div class="alert alert-danger">{{ errorMessage() }}</div>
          }

          <div class="form-group">
            <label class="form-label" for="prj-name">Nome Progetto</label>
            <input
              id="prj-name"
              type="text"
              class="form-input"
              placeholder="es. Piattaforma Web"
              formControlName="name"
              (input)="onNameInput()"
            />
            @if (form.get('name')?.invalid && form.get('name')?.touched) {
              <span class="form-error">Il nome è obbligatorio.</span>
            }
          </div>

          <div class="form-group">
            <label class="form-label" for="prj-key">Chiave Univoca (Key: 2-10 lettere)</label>
            <input
              id="prj-key"
              type="text"
              class="form-input"
              placeholder="es. WEB"
              formControlName="key"
            />
            @if (form.get('key')?.invalid && form.get('key')?.touched) {
              <span class="form-error">La chiave deve contenere tra 2 e 10 caratteri alfanumerici.</span>
            }
          </div>

          <div class="form-group">
            <label class="form-label" for="prj-desc">Descrizione (Opzionale)</label>
            <textarea
              id="prj-desc"
              rows="3"
              class="form-textarea"
              placeholder="Obiettivi e finalità del progetto"
              formControlName="description"
            ></textarea>
          </div>

          <div class="modal-actions">
            <button type="button" class="btn btn-secondary" (click)="closeCreateModal()">Annulla</button>
            <button type="submit" class="btn btn-primary" [disabled]="form.invalid || isSubmitting()">
              {{ isSubmitting() ? 'Creazione...' : 'Crea Progetto' }}
            </button>
          </div>
        </form>
      </app-modal>
    </div>
  `,
  styles: [`
    .workspace-detail-page {
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

    .page-subtitle {
      color: var(--text-secondary);
      font-size: 0.875rem;
      margin-top: 0.25rem;
    }

    .project-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
      gap: 1.25rem;
    }

    .project-card {
      padding: 1.25rem;
      cursor: pointer;
      transition: all 0.15s ease-in-out;
      display: flex;
      flex-direction: column;
      justify-content: space-between;
    }

    .project-card:hover {
      border-color: var(--color-primary);
      box-shadow: var(--shadow-md);
      transform: translateY(-2px);
    }

    .card-header {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin-bottom: 0.75rem;
    }

    .project-key {
      padding: 0.2rem 0.55rem;
      background-color: var(--color-primary-light);
      color: var(--color-primary);
      border: 1px solid var(--color-primary-border);
      border-radius: var(--radius-sm);
      font-weight: 700;
      font-size: 0.75rem;
      letter-spacing: 0.05em;
    }

    .project-title {
      font-size: 1.05rem;
      font-weight: 600;
      color: var(--text-main);
    }

    .project-description {
      color: var(--text-secondary);
      font-size: 0.875rem;
      line-height: 1.45;
      margin-bottom: 1rem;
      min-height: 2.6rem;
    }

    .card-footer {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding-top: 0.75rem;
      border-top: 1px solid var(--border-color);
      font-size: 0.75rem;
      color: var(--text-secondary);
      font-weight: 500;
    }

    .arrow {
      color: var(--color-primary);
      font-size: 1rem;
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

    .modal-actions {
      display: flex;
      justify-content: flex-end;
      gap: 0.75rem;
      margin-top: 1.5rem;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }
  `]
})
export class WorkspaceDetailComponent implements OnInit {
  readonly workspaceService = inject(WorkspaceService);
  readonly projectService = inject(ProjectService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly currentWorkspaceId = signal<string>('');
  readonly isModalOpen = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    key: ['', [Validators.required, Validators.pattern(/^[A-Za-z0-9]{2,10}$/)]],
    description: ['']
  });

  ngOnInit(): void {
    const wsId = this.route.snapshot.paramMap.get('workspaceId');
    if (wsId) {
      this.currentWorkspaceId.set(wsId);
      this.projectService.loadProjectsByWorkspace(wsId).subscribe();
    }
  }

  currentWorkspaceName(): string {
    const selected = this.workspaceService.selectedWorkspace();
    return selected ? selected.name : 'Workspace';
  }

  openCreateModal(): void {
    this.form.reset({ name: '', key: '', description: '' });
    this.errorMessage.set(null);
    this.isModalOpen.set(true);
  }

  closeCreateModal(): void {
    this.isModalOpen.set(false);
  }

  onNameInput(): void {
    const name = this.form.get('name')?.value || '';
    if (!this.form.get('key')?.dirty) {
      const key = name
        .trim()
        .replace(/[^A-Za-z0-9]/g, '')
        .substring(0, 4)
        .toUpperCase();
      if (key.length >= 2) {
        this.form.patchValue({ key });
      }
    }
  }

  onCreateProject(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const wsId = this.currentWorkspaceId();
    const req = {
      name: this.form.value.name!,
      key: this.form.value.key!.toUpperCase(),
      description: this.form.value.description
    };

    this.projectService.createProject(wsId, req).subscribe({
      next: (prj) => {
        this.isSubmitting.set(false);
        this.closeCreateModal();
        this.onSelectProject(prj);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        const msg = err.error?.detail || err.error?.message || 'Errore durante la creazione del progetto.';
        this.errorMessage.set(msg);
      }
    });
  }

  onSelectProject(prj: ProjectDto): void {
    this.router.navigate(['/projects', prj.id]);
  }
}
