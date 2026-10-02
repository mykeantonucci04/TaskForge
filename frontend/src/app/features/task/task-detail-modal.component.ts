import { Component, inject, input, output, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TaskService } from '../../core/services/task.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { TaskDetailDto, LabelDto } from '../../core/models/task.models';
import { ModalComponent } from '../../shared/components/modal/modal.component';
import { SubtaskListComponent } from './components/subtask-list/subtask-list.component';
import { CommentListComponent } from './components/comment-list/comment-list.component';
import { LabelManagerComponent } from './components/label-manager/label-manager.component';

@Component({
  selector: 'app-task-detail-modal',
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule, 
    ModalComponent,
    SubtaskListComponent,
    CommentListComponent,
    LabelManagerComponent
  ],
  templateUrl: './task-detail-modal.component.html',
  styleUrl: './task-detail-modal.component.css'
})
export class TaskDetailModalComponent {
  private readonly taskService = inject(TaskService);
  private readonly workspaceService = inject(WorkspaceService);
  private readonly fb = inject(FormBuilder);

  readonly isOpen = input<boolean>(false);
  readonly taskId = input<string | null>(null);
  readonly taskUpdated = output<void>();
  readonly close = output<void>();

  readonly isLoading = signal(false);
  readonly taskDetail = signal<TaskDetailDto | null>(null);
  readonly availableLabels = signal<LabelDto[]>([]);

  readonly isSubmittingComment = signal(false);

  readonly form = this.fb.nonNullable.group({
    title: ['', Validators.required],
    description: [''],
    priority: [1],
    dueDate: ['']
  });

  constructor() {
    effect(() => {
      const id = this.taskId();
      const open = this.isOpen();
      if (open && id) {
        this.loadTask(id);
      }
    });
  }

  loadTask(id: string): void {
    this.isLoading.set(true);
    this.taskService.getTaskById(id).subscribe({
      next: (detail) => {
        this.taskDetail.set(detail);
        this.isLoading.set(false);

        const dueDate = detail.dueDateUtc ? detail.dueDateUtc.split('T')[0] : '';
        this.form.patchValue({
          title: detail.title,
          description: detail.description || '',
          priority: detail.priority,
          dueDate
        });

        this.loadLabels();
      },
      error: () => this.isLoading.set(false)
    });
  }

  loadLabels(): void {
    const ws = this.workspaceService.selectedWorkspace();
    if (ws) {
      this.taskService.getWorkspaceLabels(ws.id).subscribe({
        next: (labels) => this.availableLabels.set(labels)
      });
    }
  }

  onAutoSave(): void {
    const task = this.taskDetail();
    if (!task || this.form.invalid) return;

    const val = this.form.getRawValue();
    const req = {
      title: val.title,
      description: val.description || undefined,
      priority: Number(val.priority),
      dueDateUtc: val.dueDate ? new Date(val.dueDate).toISOString() : undefined
    };

    this.taskService.updateTask(task.id, req).subscribe({
      next: () => this.taskUpdated.emit()
    });
  }

  onToggleSubtask(subtaskId: string): void {
    this.taskService.toggleSubtask(subtaskId).subscribe({
      next: (updated) => {
        const current = this.taskDetail();
        if (current) {
          this.taskDetail.set({
            ...current,
            subtasks: current.subtasks.map(s => s.id === subtaskId ? updated : s)
          });
          this.taskUpdated.emit();
        }
      }
    });
  }

  onDeleteSubtask(subtaskId: string): void {
    this.taskService.deleteSubtask(subtaskId).subscribe({
      next: () => {
        const current = this.taskDetail();
        if (current) {
          this.taskDetail.set({
            ...current,
            subtasks: current.subtasks.filter(s => s.id !== subtaskId)
          });
          this.taskUpdated.emit();
        }
      }
    });
  }

  onAddSubtask(title: string): void {
    const task = this.taskDetail();
    if (!title || !task) return;

    this.taskService.addSubtask(task.id, { title }).subscribe({
      next: (sub) => {
        this.taskDetail.set({
          ...task,
          subtasks: [...task.subtasks, sub]
        });
        this.taskUpdated.emit();
      }
    });
  }

  onAddComment(content: string): void {
    const task = this.taskDetail();
    if (!content || !task) return;

    this.isSubmittingComment.set(true);
    this.taskService.addComment(task.id, { content }).subscribe({
      next: (c) => {
        this.taskDetail.set({
          ...task,
          comments: [c, ...task.comments]
        });
        this.isSubmittingComment.set(false);
      },
      error: () => this.isSubmittingComment.set(false)
    });
  }

  onAssignLabel(labelId: string): void {
    const task = this.taskDetail();
    if (!labelId || !task) return;

    const label = this.availableLabels().find(l => l.id === labelId);
    if (!label) return;

    this.taskService.assignLabelToTask(task.id, labelId).subscribe({
      next: () => {
        this.taskDetail.set({
          ...task,
          labels: [...task.labels, label]
        });
        this.taskUpdated.emit();
      }
    });
  }

  onRemoveLabel(labelId: string): void {
    const task = this.taskDetail();
    if (!task) return;

    this.taskService.removeLabelFromTask(task.id, labelId).subscribe({
      next: () => {
        this.taskDetail.set({
          ...task,
          labels: task.labels.filter(l => l.id !== labelId)
        });
        this.taskUpdated.emit();
      }
    });
  }

  onCreateLabel(event: {name: string, colorHex: string}): void {
    const ws = this.workspaceService.selectedWorkspace();
    if (!event.name || !ws) return;

    this.taskService.createWorkspaceLabel(ws.id, { name: event.name, colorHex: event.colorHex }).subscribe({
      next: (newLabel) => {
        this.availableLabels.update(list => [...list, newLabel]);
        // auto-assign to current task
        this.onAssignLabel(newLabel.id);
      }
    });
  }

  onDeleteTask(): void {
    const task = this.taskDetail();
    if (!task) return;
    if (confirm('Sei sicuro di voler eliminare definitivamente questo task?')) {
      this.taskService.deleteTask(task.id).subscribe({
        next: () => {
          this.taskUpdated.emit();
          this.onClose();
        }
      });
    }
  }

  getCompletedSubtaskCount(): number {
    return this.taskDetail()?.subtasks.filter(s => s.isCompleted).length || 0;
  }

  onClose(): void {
    this.close.emit();
  }
}
