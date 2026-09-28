import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { WorkspaceService } from '../../../core/services/workspace.service';
import { WorkspaceDto } from '../../../core/models/workspace.models';
import { ModalComponent } from '../../../shared/components/modal/modal.component';

@Component({
  selector: 'app-workspace-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ModalComponent],
  template: `
    <div class="workspace-page">
      <div class="page-header">
        <div>
          <h1>Aree di Lavoro (Workspaces)</h1>
          <p class="page-subtitle">Gestisci gli spazi di lavoro e i progetti della tua organizzazione.</p>
        </div>
        <button class="btn btn-primary" (click)="openCreateModal()">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19"></line>
            <line x1="5" y1="12" x2="19" y2="12"></line>
          </svg>
          <span>Nuovo Workspace</span>
        </button>
      </div>

      @if (workspaceService.isLoading()) {
        <div class="loading-state">
          <div class="spinner"></div>
          <p>Caricamento workspace in corso...</p>
        </div>
      } @else if (workspaceService.workspaces().length === 0) {
        <div class="empty-state card">
          <div class="empty-icon">📁</div>
          <h3>Nessun Workspace presente</h3>
          <p>Crea il tuo primo spazio di lavoro per iniziare a organizzare progetti e board.</p>
          <button class="btn btn-primary" (click)="openCreateModal()">Crea Workspace</button>
        </div>
      } @else {
        <div class="workspace-grid">
          @for (ws of workspaceService.workspaces(); track ws.id) {
            <div class="workspace-card card" (click)="onSelectWorkspace(ws)">
              <div class="card-header">
                <div class="ws-avatar">{{ ws.name.substring(0, 2).toUpperCase() }}</div>
                <div class="ws-info">
                  <h3 class="ws-title">{{ ws.name }}</h3>
                  <span class="badge badge-primary">/{{ ws.slug }}</span>
                </div>
              </div>
              <p class="ws-description">{{ ws.description || 'Nessuna descrizione specificata.' }}</p>
              <div class="card-footer">
                <span class="ws-date">Creato il {{ formatDate(ws.createdAtUtc) }}</span>
                <span class="ws-arrow">→</span>
              </div>
            </div>
          }
        </div>
      }

      <!-- Modal Crea Workspace -->
      <app-modal [isOpen]="isModalOpen()" [title]="'Crea Nuovo Workspace'" (close)="closeCreateModal()">
        <form [formGroup]="form" (ngSubmit)="onCreateWorkspace()">
          @if (errorMessage()) {
            <div class="alert alert-danger">{{ errorMessage() }}</div>
          }

          <div class="form-group">
            <label class="form-label" for="ws-name">Nome Workspace</label>
            <input
              id="ws-name"
              type="text"
              class="form-input"
              placeholder="es. Sviluppo Software"
              formControlName="name"
              (input)="onNameInput()"
            />
            @if (form.get('name')?.invalid && form.get('name')?.touched) {
              <span class="form-error">Il nome è obbligatorio.</span>
            }
          </div>

          <div class="form-group">
            <label class="form-label" for="ws-slug">Slug Univoco (URL)</label>
            <input
              id="ws-slug"
              type="text"
              class="form-input"
              placeholder="es. sviluppo-software"
              formControlName="slug"
            />
            @if (form.get('slug')?.invalid && form.get('slug')?.touched) {
              <span class="form-error">Lo slug può contenere solo lettere minuscole, numeri e trattini.</span>
            }
          </div>

          <div class="form-group">
            <label class="form-label" for="ws-desc">Descrizione (Opzionale)</label>
            <textarea
              id="ws-desc"
              rows="3"
              class="form-textarea"
              placeholder="Breve descrizione degli obiettivi di questo workspace"
              formControlName="description"
            ></textarea>
          </div>

          <div class="modal-actions">
            <button type="button" class="btn btn-secondary" (click)="closeCreateModal()">Annulla</button>
            <button type="submit" class="btn btn-primary" [disabled]="form.invalid || isSubmitting()">
              {{ isSubmitting() ? 'Creazione...' : 'Crea Workspace' }}
            </button>
          </div>
        </form>
      </app-modal>
    </div>
  `,
  styles: [`
    .workspace-page {
      max-width: 1100px;
      margin: 0 auto;
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
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

    .workspace-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
      gap: 1.25rem;
    }

    .workspace-card {
      padding: 1.25rem;
      cursor: pointer;
      transition: all 0.15s ease-in-out;
      display: flex;
      flex-direction: column;
      justify-content: space-between;
    }

    .workspace-card:hover {
      border-color: var(--color-primary);
      box-shadow: var(--shadow-md);
      transform: translateY(-2px);
    }

    .card-header {
      display: flex;
      align-items: center;
      gap: 0.85rem;
      margin-bottom: 0.75rem;
    }

    .ws-avatar {
      width: 42px;
      height: 42px;
      border-radius: var(--radius-md);
      background-color: var(--color-primary-light);
      color: var(--color-primary);
      font-weight: 700;
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 1rem;
      border: 1px solid var(--color-primary-border);
    }

    .ws-title {
      font-size: 1.05rem;
      font-weight: 600;
      color: var(--text-main);
    }

    .ws-description {
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
      color: var(--text-muted);
    }

    .ws-arrow {
      font-weight: 600;
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
export class WorkspaceListComponent implements OnInit {
  readonly workspaceService = inject(WorkspaceService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly isModalOpen = signal(false);
  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    slug: ['', [Validators.required, Validators.pattern(/^[a-z0-9-]+$/)]],
    description: ['']
  });

  ngOnInit(): void {
    this.workspaceService.loadWorkspaces().subscribe();
  }

  openCreateModal(): void {
    this.form.reset({ name: '', slug: '', description: '' });
    this.errorMessage.set(null);
    this.isModalOpen.set(true);
  }

  closeCreateModal(): void {
    this.isModalOpen.set(false);
  }

  onNameInput(): void {
    const name = this.form.get('name')?.value || '';
    const slug = name
      .toLowerCase()
      .trim()
      .replace(/[\s_]+/g, '-')
      .replace(/[^a-z0-9-]/g, '');
    this.form.patchValue({ slug });
  }

  onCreateWorkspace(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.workspaceService.createWorkspace(this.form.getRawValue()).subscribe({
      next: (ws) => {
        this.isSubmitting.set(false);
        this.closeCreateModal();
        this.onSelectWorkspace(ws);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        const msg = err.error?.detail || err.error?.message || 'Errore durante la creazione del workspace.';
        this.errorMessage.set(msg);
      }
    });
  }

  onSelectWorkspace(ws: WorkspaceDto): void {
    this.workspaceService.selectWorkspace(ws);
    this.router.navigate(['/workspaces', ws.id]);
  }

  formatDate(utcStr: string): string {
    try {
      const d = new Date(utcStr);
      return d.toLocaleDateString();
    } catch {
      return utcStr;
    }
  }
}
