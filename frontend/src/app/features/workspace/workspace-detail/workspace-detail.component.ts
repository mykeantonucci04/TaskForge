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
  templateUrl: './workspace-detail.component.html',
  styleUrl: './workspace-detail.component.css'
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
