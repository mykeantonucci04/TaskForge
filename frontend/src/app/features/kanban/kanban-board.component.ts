import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CdkDragDrop, DragDropModule, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { BoardService } from '../../core/services/board.service';
import { TaskService } from '../../core/services/task.service';
import { BoardColumnDetailDto, TaskItemStatus } from '../../core/models/board.models';
import { TaskDto, TaskPriority } from '../../core/models/task.models';
import { TaskDetailModalComponent } from '../task/task-detail-modal.component';

@Component({
  selector: 'app-kanban-board',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, DragDropModule, TaskDetailModalComponent],
  template: `
    <div class="kanban-page">
      <!-- Breadcrumb Header -->
      <nav class="breadcrumb">
        <a routerLink="/workspaces" class="breadcrumb-link">Workspaces</a>
        <span class="breadcrumb-separator">/</span>
        <span class="breadcrumb-current">{{ boardService.currentBoard()?.name || 'Lavagna Kanban' }}</span>
      </nav>

      <div class="board-header">
        <div class="board-title-area">
          <div class="board-tag">KANBAN</div>
          <h1>{{ boardService.currentBoard()?.name }}</h1>
        </div>
      </div>

      @if (boardService.isLoading()) {
        <div class="loading-state">
          <div class="spinner"></div>
          <p>Caricamento board Kanban...</p>
        </div>
      } @else if (boardService.currentBoard()) {
        <!-- Kanban Columns Board Container -->
        <div class="board-canvas" cdkDropListGroup>
          @for (col of boardService.currentBoard()?.columns; track col.id) {
            <div class="kanban-column card">
              <div class="column-header">
                <div class="column-title-group">
                  <span class="column-title">{{ col.name }}</span>
                  <span class="column-count">{{ col.tasks.length }}</span>
                </div>
              </div>

              <!-- Task List with CDK DropList -->
              <div
                class="task-drop-list"
                cdkDropList
                [cdkDropListData]="col.tasks"
                [id]="col.id"
                (cdkDropListDropped)="onTaskDrop($event, col)"
              >
                @for (task of col.tasks; track task.id) {
                  <div
                    class="task-card card"
                    cdkDrag
                    (click)="openTaskDetail(task.id)"
                  >
                    <div class="task-card-header">
                      <span class="priority-badge" [ngClass]="getPriorityClass(task.priority)">
                        {{ getPriorityLabel(task.priority) }}
                      </span>
                    </div>

                    <h4 class="task-title">{{ task.title }}</h4>

                    @if (task.description) {
                      <p class="task-preview-desc">{{ task.description }}</p>
                    }

                    <div class="task-card-footer">
                      @if (task.dueDateUtc) {
                        <span class="task-due-date">📅 {{ formatDue(task.dueDateUtc) }}</span>
                      }
                    </div>
                  </div>
                }

                @if (col.tasks.length === 0) {
                  <div class="empty-column-placeholder">
                    Trascina qui i task
                  </div>
                }
              </div>

              <!-- Quick Add Task in Column -->
              <div class="column-footer">
                @if (activeAddColumnId() === col.id) {
                  <form class="quick-add-form" (ngSubmit)="onCreateQuickTask(col)">
                    <input
                      type="text"
                      class="form-input input-sm"
                      placeholder="Cosa c'è da fare?"
                      [value]="quickTaskTitle()"
                      (input)="quickTaskTitle.set($any($event.target).value)"
                      autoFocus
                    />
                    <div class="quick-add-actions">
                      <button type="submit" class="btn btn-primary btn-sm" [disabled]="!quickTaskTitle().trim()">
                        Aggiungi
                      </button>
                      <button type="button" class="btn btn-secondary btn-sm" (click)="cancelQuickAdd()">
                        Annulla
                      </button>
                    </div>
                  </form>
                } @else {
                  <button class="btn-quick-add" (click)="startQuickAdd(col.id)">
                    + Aggiungi Task
                  </button>
                }
              </div>
            </div>
          }
        </div>
      }

      <!-- Task Detail Modal Dialog -->
      <app-task-detail-modal
        [isOpen]="isTaskModalOpen()"
        [taskId]="selectedTaskId()"
        (taskUpdated)="onReloadBoard()"
        (close)="closeTaskModal()"
      ></app-task-detail-modal>
    </div>
  `,
  styles: [`
    .kanban-page {
      display: flex;
      flex-direction: column;
      height: 100%;
      overflow: hidden;
    }

    .breadcrumb {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.8125rem;
      color: var(--text-secondary);
      margin-bottom: 0.75rem;
    }

    .breadcrumb-link {
      color: var(--color-primary);
      text-decoration: none;
    }

    .board-header {
      margin-bottom: 1.25rem;
    }

    .board-title-area {
      display: flex;
      align-items: center;
      gap: 0.75rem;
    }

    .board-tag {
      font-size: 0.65rem;
      font-weight: 700;
      color: var(--color-primary);
      background-color: var(--color-primary-light);
      border: 1px solid var(--color-primary-border);
      padding: 0.15rem 0.4rem;
      border-radius: var(--radius-sm);
      letter-spacing: 0.05em;
    }

    .board-canvas {
      display: flex;
      gap: 1.25rem;
      overflow-x: auto;
      flex: 1;
      padding-bottom: 1.5rem;
      align-items: flex-start;
    }

    .kanban-column {
      width: 320px;
      min-width: 320px;
      max-height: calc(100vh - 170px);
      display: flex;
      flex-direction: column;
      background-color: var(--bg-subtle);
      border: 1px solid var(--border-color);
      border-radius: var(--radius-lg);
      padding: 0.75rem;
    }

    .column-header {
      padding: 0.25rem 0.5rem 0.75rem;
    }

    .column-title-group {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .column-title {
      font-weight: 600;
      font-size: 0.95rem;
      color: var(--text-main);
    }

    .column-count {
      background-color: var(--bg-surface);
      border: 1px solid var(--border-color);
      color: var(--text-secondary);
      font-size: 0.75rem;
      font-weight: 600;
      padding: 0.1rem 0.45rem;
      border-radius: 9999px;
    }

    .task-drop-list {
      flex: 1;
      overflow-y: auto;
      display: flex;
      flex-direction: column;
      gap: 0.65rem;
      min-height: 120px;
      padding: 0.25rem;
    }

    .task-card {
      padding: 0.85rem;
      background-color: var(--bg-surface);
      cursor: grab;
      transition: box-shadow 0.15s ease, border-color 0.15s ease;
      display: flex;
      flex-direction: column;
      gap: 0.45rem;
    }

    .task-card:hover {
      border-color: #cbd5e1;
      box-shadow: var(--shadow-md);
    }

    .task-card:active {
      cursor: grabbing;
    }

    .cdk-drag-preview {
      box-shadow: var(--shadow-lg);
      border-radius: var(--radius-md);
      background-color: var(--bg-surface);
      padding: 0.85rem;
      opacity: 0.95;
    }

    .cdk-drag-placeholder {
      opacity: 0.25;
      background: var(--bg-subtle);
      border: 1px dashed var(--border-color);
    }

    .task-title {
      font-size: 0.9rem;
      font-weight: 600;
      color: var(--text-main);
      line-height: 1.35;
    }

    .task-preview-desc {
      font-size: 0.775rem;
      color: var(--text-secondary);
      line-height: 1.35;
      overflow: hidden;
      text-overflow: ellipsis;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
    }

    .priority-badge {
      display: inline-block;
      font-size: 0.65rem;
      font-weight: 600;
      padding: 0.1rem 0.4rem;
      border-radius: var(--radius-sm);
      text-transform: uppercase;
    }

    .priority-low {
      background-color: #f1f5f9;
      color: #64748b;
    }

    .priority-medium {
      background-color: #eff6ff;
      color: #3b82f6;
    }

    .priority-high {
      background-color: #fffbeb;
      color: #d97706;
    }

    .priority-urgent {
      background-color: #fef2f2;
      color: #ef4444;
    }

    .task-card-footer {
      display: flex;
      align-items: center;
      justify-content: space-between;
      font-size: 0.725rem;
      color: var(--text-muted);
    }

    .empty-column-placeholder {
      border: 1px dashed var(--border-color);
      border-radius: var(--radius-md);
      padding: 1.5rem 1rem;
      text-align: center;
      font-size: 0.8rem;
      color: var(--text-muted);
    }

    .column-footer {
      padding-top: 0.65rem;
      border-top: 1px solid var(--border-color);
      margin-top: 0.5rem;
    }

    .btn-quick-add {
      width: 100%;
      text-align: left;
      background: none;
      border: none;
      color: var(--text-secondary);
      font-size: 0.825rem;
      font-weight: 500;
      padding: 0.4rem 0.5rem;
      border-radius: var(--radius-sm);
      cursor: pointer;
    }

    .btn-quick-add:hover {
      background-color: var(--bg-surface);
      color: var(--text-main);
    }

    .quick-add-form {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .quick-add-actions {
      display: flex;
      gap: 0.5rem;
    }

    .loading-state {
      padding: 4rem;
      text-align: center;
    }

    .spinner {
      width: 32px;
      height: 32px;
      border: 3px solid var(--border-color);
      border-top-color: var(--color-primary);
      border-radius: 50%;
      animation: spin 0.8s linear infinite;
      margin: 0 auto 1rem;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }
  `]
})
export class KanbanBoardComponent implements OnInit {
  readonly boardService = inject(BoardService);
  private readonly taskService = inject(TaskService);
  private readonly route = inject(ActivatedRoute);

  readonly boardId = signal<string>('');
  readonly isTaskModalOpen = signal(false);
  readonly selectedTaskId = signal<string | null>(null);

  readonly activeAddColumnId = signal<string | null>(null);
  readonly quickTaskTitle = signal<string>('');

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('boardId');
    if (id) {
      this.boardId.set(id);
      this.boardService.getBoardDetails(id).subscribe();
    }
  }

  onReloadBoard(): void {
    const id = this.boardId();
    if (id) {
      this.boardService.getBoardDetails(id).subscribe();
    }
  }

  onTaskDrop(event: CdkDragDrop<TaskDto[]>, targetCol: BoardColumnDetailDto): void {
    if (event.previousContainer === event.container) {
      // Reorder within same column
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      // Transfer to different column
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
    }

    const movedTask = event.container.data[event.currentIndex];
    const newPosition = this.calculateFractionalPosition(event.container.data, event.currentIndex);

    // Call backend MoveTaskCommand
    this.taskService.moveTask(movedTask.id, {
      targetColumnId: targetCol.id,
      newPosition: newPosition,
      newStatus: targetCol.targetStatus
    }).subscribe({
      next: (updatedTask) => {
        movedTask.position = updatedTask.position;
        movedTask.status = updatedTask.status;
        movedTask.columnId = updatedTask.columnId;
      },
      error: () => this.onReloadBoard()
    });
  }

  private calculateFractionalPosition(tasks: TaskDto[], index: number): number {
    if (tasks.length <= 1) return 1000.0;
    if (index === 0) {
      const nextPos = tasks[1]?.position || 1000.0;
      return nextPos / 2.0;
    }
    if (index === tasks.length - 1) {
      const prevPos = tasks[index - 1]?.position || 0.0;
      return prevPos + 1000.0;
    }
    const prevPos = tasks[index - 1]?.position || 0.0;
    const nextPos = tasks[index + 1]?.position || prevPos + 2000.0;
    return (prevPos + nextPos) / 2.0;
  }

  startQuickAdd(columnId: string): void {
    this.activeAddColumnId.set(columnId);
    this.quickTaskTitle.set('');
  }

  cancelQuickAdd(): void {
    this.activeAddColumnId.set(null);
    this.quickTaskTitle.set('');
  }

  onCreateQuickTask(column: BoardColumnDetailDto): void {
    const title = this.quickTaskTitle().trim();
    if (!title) return;

    this.taskService.createTask({
      columnId: column.id,
      title: title
    }).subscribe({
      next: () => {
        this.cancelQuickAdd();
        this.onReloadBoard();
      }
    });
  }

  openTaskDetail(taskId: string): void {
    this.selectedTaskId.set(taskId);
    this.isTaskModalOpen.set(true);
  }

  closeTaskModal(): void {
    this.isTaskModalOpen.set(false);
    this.selectedTaskId.set(null);
  }

  getPriorityLabel(priority: TaskPriority): string {
    switch (priority) {
      case TaskPriority.Low: return 'Bassa';
      case TaskPriority.Medium: return 'Media';
      case TaskPriority.High: return 'Alta';
      case TaskPriority.Urgent: return 'Urgente';
      default: return 'Normale';
    }
  }

  getPriorityClass(priority: TaskPriority): string {
    switch (priority) {
      case TaskPriority.Low: return 'priority-low';
      case TaskPriority.Medium: return 'priority-medium';
      case TaskPriority.High: return 'priority-high';
      case TaskPriority.Urgent: return 'priority-urgent';
      default: return 'priority-medium';
    }
  }

  formatDue(utcStr: string): string {
    try {
      const d = new Date(utcStr);
      return d.toLocaleDateString();
    } catch {
      return utcStr;
    }
  }
}
