import { Component, inject, input, output, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TaskService } from '../../core/services/task.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { TaskDetailDto, TaskPriority, LabelDto } from '../../core/models/task.models';
import { ModalComponent } from '../../shared/components/modal/modal.component';

@Component({
  selector: 'app-task-detail-modal',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ModalComponent],
  template: `
    <app-modal [isOpen]="isOpen()" [title]="'Dettaglio Task'" (close)="onClose()">
      @if (isLoading()) {
        <div class="loading-state">
          <div class="spinner"></div>
          <p>Caricamento dettagli task...</p>
        </div>
      } @else if (taskDetail(); as task) {
        <div class="task-modal-content">
          <!-- Main Left Column -->
          <div class="task-main">
            <!-- Title & Description Form -->
            <form [formGroup]="form" (ngSubmit)="onAutoSave()">
              <div class="form-group">
                <label class="form-label" for="task-title">Titolo</label>
                <input
                  id="task-title"
                  type="text"
                  class="form-input title-input"
                  formControlName="title"
                  (blur)="onAutoSave()"
                />
              </div>

              <div class="form-group">
                <label class="form-label" for="task-desc">Descrizione</label>
                <textarea
                  id="task-desc"
                  rows="3"
                  class="form-textarea"
                  placeholder="Aggiungi una descrizione più dettagliata..."
                  formControlName="description"
                  (blur)="onAutoSave()"
                ></textarea>
              </div>

              <div class="meta-row">
                <div class="form-group">
                  <label class="form-label" for="task-priority">Priorità</label>
                  <select
                    id="task-priority"
                    class="form-select"
                    formControlName="priority"
                    (change)="onAutoSave()"
                  >
                    <option [value]="1">Bassa</option>
                    <option [value]="2">Media</option>
                    <option [value]="3">Alta</option>
                    <option [value]="4">Urgente</option>
                  </select>
                </div>

                <div class="form-group">
                  <label class="form-label" for="task-due">Scadenza</label>
                  <input
                    id="task-due"
                    type="date"
                    class="form-input"
                    formControlName="dueDate"
                    (change)="onAutoSave()"
                  />
                </div>
              </div>
            </form>

            <!-- Subtasks Section -->
            <section class="section subtasks-section">
              <div class="section-header">
                <h4>Checklist / Sotto-task ({{ getCompletedSubtaskCount() }}/{{ task.subtasks.length }})</h4>
              </div>

              <div class="subtask-list">
                @for (sub of task.subtasks; track sub.id) {
                  <div class="subtask-item">
                    <label class="checkbox-container">
                      <input
                        type="checkbox"
                        [checked]="sub.isCompleted"
                        (change)="onToggleSubtask(sub.id)"
                      />
                      <span class="subtask-title" [class.completed]="sub.isCompleted">{{ sub.title }}</span>
                    </label>
                    <button class="btn-icon-danger" (click)="onDeleteSubtask(sub.id)" title="Elimina">×</button>
                  </div>
                }
              </div>

              <div class="add-subtask-form">
                <input
                  type="text"
                  class="form-input input-sm"
                  placeholder="+ Aggiungi elemento alla checklist..."
                  [value]="newSubtaskTitle()"
                  (input)="newSubtaskTitle.set($any($event.target).value)"
                  (keydown.enter)="onAddSubtask()"
                />
                <button type="button" class="btn btn-secondary btn-sm" [disabled]="!newSubtaskTitle().trim()" (click)="onAddSubtask()">
                  Aggiungi
                </button>
              </div>
            </section>

            <!-- Comments Section -->
            <section class="section comments-section">
              <div class="section-header">
                <h4>Attività e Commenti</h4>
              </div>

              <div class="comments-list">
                @for (c of task.comments; track c.id) {
                  <div class="comment-item">
                    <div class="comment-header">
                      <span class="comment-author">Utente</span>
                      <span class="comment-date">{{ formatDate(c.createdAtUtc) }}</span>
                    </div>
                    <p class="comment-content">{{ c.content }}</p>
                  </div>
                }
              </div>

              <div class="add-comment-box">
                <textarea
                  rows="2"
                  class="form-textarea"
                  placeholder="Scrivi un commento..."
                  [value]="newCommentText()"
                  (input)="newCommentText.set($any($event.target).value)"
                ></textarea>
                <button
                  type="button"
                  class="btn btn-primary btn-sm"
                  [disabled]="!newCommentText().trim() || isSubmittingComment()"
                  (click)="onAddComment()"
                >
                  Invia Commento
                </button>
              </div>
            </section>
          </div>

          <!-- Sidebar / Labels Column -->
          <div class="task-sidebar">
            <div class="sidebar-section">
              <h4>Etichette</h4>
              <div class="task-labels">
                @for (label of task.labels; track label.id) {
                  <span
                    class="label-badge"
                    [style.background-color]="label.colorHex + '25'"
                    [style.border-color]="label.colorHex"
                    [style.color]="label.colorHex"
                  >
                    {{ label.name }}
                    <button class="label-remove" (click)="onRemoveLabel(label.id)">×</button>
                  </span>
                }
              </div>

              <!-- Assign Available Label -->
              <div class="label-actions">
                <select class="form-select select-sm" #labelSelect (change)="onAssignLabel(labelSelect.value); labelSelect.value=''">
                  <option value="">+ Assegna etichetta...</option>
                  @for (l of availableLabels(); track l.id) {
                    <option [value]="l.id">{{ l.name }}</option>
                  }
                </select>
              </div>

              <!-- Create new label form -->
              <div class="new-label-box">
                <input
                  type="text"
                  class="form-input input-sm"
                  placeholder="Nuova etichetta..."
                  [value]="newLabelName()"
                  (input)="newLabelName.set($any($event.target).value)"
                />
                <div class="color-picker-row">
                  <input
                    type="color"
                    class="color-input"
                    [value]="newLabelColor()"
                    (input)="newLabelColor.set($any($event.target).value)"
                  />
                  <button
                    type="button"
                    class="btn btn-secondary btn-sm"
                    [disabled]="!newLabelName().trim()"
                    (click)="onCreateLabel()"
                  >
                    Crea
                  </button>
                </div>
              </div>
            </div>

            <!-- Danger Zone -->
            <div class="sidebar-section danger-zone">
              <h4>Azioni</h4>
              <button class="btn btn-danger btn-block btn-sm" (click)="onDeleteTask()">
                🗑️ Elimina Task
              </button>
            </div>
          </div>
        </div>
      }
    </app-modal>
  `,
  styles: [`
    .task-modal-content {
      display: grid;
      grid-template-columns: 1fr 220px;
      gap: 1.5rem;
    }

    .title-input {
      font-size: 1.15rem;
      font-weight: 600;
    }

    .meta-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
      margin-top: 0.5rem;
    }

    .section {
      margin-top: 1.5rem;
      padding-top: 1.25rem;
      border-top: 1px solid var(--border-color);
    }

    .section-header h4 {
      font-size: 0.95rem;
      font-weight: 600;
      color: var(--text-main);
      margin-bottom: 0.75rem;
    }

    .subtask-list {
      display: flex;
      flex-direction: column;
      gap: 0.4rem;
      margin-bottom: 0.75rem;
    }

    .subtask-item {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0.4rem 0.6rem;
      background-color: var(--bg-subtle);
      border-radius: var(--radius-sm);
    }

    .checkbox-container {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      cursor: pointer;
      font-size: 0.875rem;
    }

    .subtask-title.completed {
      text-decoration: line-through;
      color: var(--text-muted);
    }

    .btn-icon-danger {
      background: none;
      border: none;
      color: var(--text-muted);
      cursor: pointer;
      font-size: 1.1rem;
      line-height: 1;
    }

    .btn-icon-danger:hover {
      color: var(--color-danger);
    }

    .add-subtask-form {
      display: flex;
      gap: 0.5rem;
    }

    .input-sm {
      padding: 0.35rem 0.5rem;
      font-size: 0.8125rem;
    }

    .select-sm {
      padding: 0.35rem 0.5rem;
      font-size: 0.8125rem;
    }

    .comments-list {
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
      margin-bottom: 1rem;
      max-height: 200px;
      overflow-y: auto;
    }

    .comment-item {
      padding: 0.6rem 0.75rem;
      background-color: var(--bg-subtle);
      border-radius: var(--radius-md);
    }

    .comment-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      font-size: 0.75rem;
      margin-bottom: 0.25rem;
    }

    .comment-author {
      font-weight: 600;
      color: var(--text-main);
    }

    .comment-date {
      color: var(--text-muted);
    }

    .comment-content {
      font-size: 0.8125rem;
      color: var(--text-secondary);
    }

    .add-comment-box {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      align-items: flex-end;
    }

    .add-comment-box textarea {
      width: 100%;
    }

    .task-sidebar {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
      border-left: 1px solid var(--border-color);
      padding-left: 1.25rem;
    }

    .sidebar-section h4 {
      font-size: 0.85rem;
      font-weight: 600;
      color: var(--text-secondary);
      margin-bottom: 0.6rem;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }

    .task-labels {
      display: flex;
      flex-wrap: wrap;
      gap: 0.4rem;
      margin-bottom: 0.75rem;
    }

    .label-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
      padding: 0.15rem 0.45rem;
      border-radius: var(--radius-sm);
      font-size: 0.75rem;
      font-weight: 600;
      border: 1px solid;
    }

    .label-remove {
      background: none;
      border: none;
      font-size: 0.95rem;
      color: inherit;
      cursor: pointer;
      line-height: 1;
      padding: 0;
    }

    .color-picker-row {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      margin-top: 0.4rem;
    }

    .color-input {
      width: 32px;
      height: 32px;
      border: none;
      cursor: pointer;
      background: none;
    }

    .danger-zone {
      margin-top: auto;
      padding-top: 1rem;
      border-top: 1px solid var(--border-color);
    }

    .loading-state {
      padding: 2rem;
      text-align: center;
    }

    .spinner {
      width: 28px;
      height: 28px;
      border: 3px solid var(--border-color);
      border-top-color: var(--color-primary);
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
      margin: 0 auto 0.75rem;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }
  `]
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

  readonly newSubtaskTitle = signal('');
  readonly newCommentText = signal('');
  readonly isSubmittingComment = signal(false);
  readonly newLabelName = signal('');
  readonly newLabelColor = signal('#2563eb');

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

  onAddSubtask(): void {
    const title = this.newSubtaskTitle().trim();
    const task = this.taskDetail();
    if (!title || !task) return;

    this.taskService.addSubtask(task.id, { title }).subscribe({
      next: (sub) => {
        this.taskDetail.set({
          ...task,
          subtasks: [...task.subtasks, sub]
        });
        this.newSubtaskTitle.set('');
        this.taskUpdated.emit();
      }
    });
  }

  onAddComment(): void {
    const content = this.newCommentText().trim();
    const task = this.taskDetail();
    if (!content || !task) return;

    this.isSubmittingComment.set(true);
    this.taskService.addComment(task.id, { content }).subscribe({
      next: (c) => {
        this.taskDetail.set({
          ...task,
          comments: [c, ...task.comments]
        });
        this.newCommentText.set('');
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

  onCreateLabel(): void {
    const name = this.newLabelName().trim();
    const color = this.newLabelColor();
    const ws = this.workspaceService.selectedWorkspace();
    if (!name || !ws) return;

    this.taskService.createWorkspaceLabel(ws.id, { name, colorHex: color }).subscribe({
      next: (newLabel) => {
        this.availableLabels.update(list => [...list, newLabel]);
        this.newLabelName.set('');
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

  formatDate(utcStr: string): string {
    try {
      const d = new Date(utcStr);
      return d.toLocaleDateString() + ' ' + d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    } catch {
      return utcStr;
    }
  }

  onClose(): void {
    this.close.emit();
  }
}
