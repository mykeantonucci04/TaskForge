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
  templateUrl: './workspace-list.component.html',
  styleUrl: './workspace-list.component.css'
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
