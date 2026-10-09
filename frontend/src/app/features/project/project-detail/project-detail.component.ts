import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ProjectService } from '../../../core/services/project.service';
import { WorkspaceService } from '../../../core/services/workspace.service';
import { BoardSummaryDto, UpdateProjectRequest } from '../../../core/models/project.models';
import { ModalComponent } from '../../../shared/components/modal/modal.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-project-detail',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, ModalComponent, ConfirmDialogComponent],
  templateUrl: './project-detail.component.html',
  styleUrl: './project-detail.component.css'
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

  // Edit Project State
  readonly isEditModalOpen = signal(false);
  readonly isSubmittingEdit = signal(false);
  readonly editErrorMessage = signal<string | null>(null);

  // Delete Project State
  readonly isDeleteConfirmOpen = signal(false);
  readonly isDeleting = signal(false);
  readonly deleteErrorMessage = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    createDefaultColumns: [true]
  });

  readonly editForm = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['']
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

  openEditModal(): void {
    const current = this.projectService.currentProject();
    this.editForm.reset({
      name: current?.name || '',
      description: current?.description || ''
    });
    this.editErrorMessage.set(null);
    this.isEditModalOpen.set(true);
  }

  closeEditModal(): void {
    this.isEditModalOpen.set(false);
  }

  onUpdateProject(): void {
    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    this.isSubmittingEdit.set(true);
    this.editErrorMessage.set(null);

    const pId = this.projectId();
    const req: UpdateProjectRequest = {
      name: this.editForm.value.name!,
      description: this.editForm.value.description ?? ''
    };

    this.projectService.updateProject(pId, req).subscribe({
      next: () => {
        this.isSubmittingEdit.set(false);
        this.closeEditModal();
      },
      error: (err) => {
        this.isSubmittingEdit.set(false);
        const msg = err.error?.detail || err.error?.message || 'Errore durante la modifica del progetto.';
        this.editErrorMessage.set(msg);
      }
    });
  }

  openDeleteConfirm(): void {
    this.deleteErrorMessage.set(null);
    this.isDeleteConfirmOpen.set(true);
  }

  closeDeleteConfirm(): void {
    this.isDeleteConfirmOpen.set(false);
  }

  onConfirmDelete(): void {
    this.isDeleting.set(true);
    this.deleteErrorMessage.set(null);
    const pId = this.projectId();
    const targetWsId = this.projectService.currentProject()?.workspaceId || this.workspaceService.selectedWorkspace()?.id;

    this.projectService.deleteProject(pId).subscribe({
      next: () => {
        this.isDeleting.set(false);
        this.closeDeleteConfirm();
        const wsId = targetWsId || this.projectService.currentProject()?.workspaceId || this.workspaceService.selectedWorkspace()?.id;
        if (wsId) {
          this.router.navigate(['/workspaces', wsId]);
        } else {
          this.router.navigate(['/workspaces']);
        }
      },
      error: (err) => {
        this.isDeleting.set(false);
        const msg = err.error?.detail || err.error?.message || 'Errore durante l\'eliminazione del progetto.';
        this.deleteErrorMessage.set(msg);
      }
    });
  }

  onSelectBoard(board: BoardSummaryDto): void {
    this.router.navigate(['/boards', board.id]);
  }
}
