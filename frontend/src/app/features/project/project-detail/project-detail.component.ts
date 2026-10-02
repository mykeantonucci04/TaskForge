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
