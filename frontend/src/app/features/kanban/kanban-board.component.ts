import { Component, inject, OnInit, OnDestroy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CdkDragDrop, DragDropModule, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { Subscription } from 'rxjs';
import { BoardService } from '../../core/services/board.service';
import { TaskService } from '../../core/services/task.service';
import {
  BoardColumnDetailDto,
  BoardDto,
  ReorderColumnsRequest,
  TaskItemStatus
} from '../../core/models/board.models';
import { TaskDto, TaskPriority } from '../../core/models/task.models';
import { TaskDetailModalComponent } from '../task/task-detail-modal.component';
import { ModalComponent } from '../../shared/components/modal/modal.component';

@Component({
  selector: 'app-kanban-board',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    DragDropModule,
    TaskDetailModalComponent,
    ModalComponent
  ],
  template: `
    <div class="kanban-page">
      <!-- Breadcrumb Header -->
      <nav class="breadcrumb">
        <a routerLink="/workspaces" class="breadcrumb-link">Workspaces</a>
        <span class="breadcrumb-separator">/</span>
        @if (boardService.currentBoard()?.projectId) {
          <a [routerLink]="['/projects', boardService.currentBoard()?.projectId]" class="breadcrumb-link">Progetto</a>
          <span class="breadcrumb-separator">/</span>
        }
        <span class="breadcrumb-current">{{ boardService.currentBoard()?.name || 'Lavagna Kanban' }}</span>
      </nav>

      <div class="board-header">
        <div class="board-header-top">
          <div class="board-title-area">
            <div class="board-tag">KANBAN</div>
            <h1>{{ boardService.currentBoard()?.name }}</h1>
          </div>

          <div class="board-header-actions">
            <button type="button" class="btn btn-secondary btn-sm" (click)="openCreateColumnModal()">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="12" y1="5" x2="12" y2="19"></line>
                <line x1="5" y1="12" x2="19" y2="12"></line>
              </svg>
              <span>+ Nuova Colonna</span>
            </button>
            <button type="button" class="btn btn-primary btn-sm" (click)="openCreateBoardModal()">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <line x1="12" y1="5" x2="12" y2="19"></line>
                <line x1="5" y1="12" x2="19" y2="12"></line>
              </svg>
              <span>+ Nuova Board</span>
            </button>
          </div>
        </div>

        <!-- Multi-board Navigation Switcher -->
        @if (projectBoards().length > 0) {
          <div class="board-nav-bar">
            <span class="board-nav-label">Lavagne:</span>
            <div class="board-pills-container">
              @for (b of projectBoards(); track b.id) {
                <button
                  type="button"
                  class="board-pill"
                  [class.active]="b.id === boardId()"
                  (click)="onSelectBoard(b.id)"
                >
                  <span class="pill-icon">📋</span>
                  <span class="pill-name">{{ b.name }}</span>
                </button>
              }
            </div>
            <div class="board-select-wrapper">
              <select
                class="form-select select-sm"
                [value]="boardId()"
                (change)="onBoardSelectChange($event)"
                aria-label="Seleziona Board"
              >
                @for (b of projectBoards(); track b.id) {
                  <option [value]="b.id">{{ b.name }}</option>
                }
              </select>
            </div>
          </div>
        }
      </div>

      @if (boardService.isLoading()) {
        <div class="loading-state">
          <div class="spinner"></div>
          <p>Caricamento board Kanban...</p>
        </div>
      } @else if (boardService.currentBoard()) {
        <!-- Kanban Columns Board Container -->
        <div class="board-canvas" cdkDropListGroup>
          @for (col of boardService.currentBoard()?.columns; track col.id; let index = $index) {
            <div class="kanban-column card">
              <div class="column-header">
                <div class="column-title-group">
                  <span class="column-title">{{ col.name }}</span>
                  <span class="column-count">{{ col.tasks.length }}</span>
                </div>
                <div class="column-actions">
                  <button
                    type="button"
                    class="btn-col-action"
                    [disabled]="index === 0"
                    (click)="onMoveColumnLeft(col, index)"
                    title="Sposta colonna a sinistra"
                    aria-label="Sposta colonna a sinistra"
                  >
                    &larr;
                  </button>
                  <button
                    type="button"
                    class="btn-col-action"
                    [disabled]="index >= (boardService.currentBoard()?.columns?.length || 1) - 1"
                    (click)="onMoveColumnRight(col, index)"
                    title="Sposta colonna a destra"
                    aria-label="Sposta colonna a destra"
                  >
                    &rarr;
                  </button>
                  <button
                    type="button"
                    class="btn-col-action btn-delete-col"
                    (click)="confirmDeleteColumn(col)"
                    title="Elimina colonna"
                    aria-label="Elimina colonna"
                  >
                    <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                      <path d="M3 6h18M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path>
                    </svg>
                  </button>
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
                  <div class="quick-add-form">
                    <input
                      type="text"
                      class="form-input input-sm"
                      placeholder="Cosa c'è da fare?"
                      [value]="quickTaskTitle()"
                      (input)="quickTaskTitle.set($any($event.target).value)"
                      (keydown.enter)="onCreateQuickTask(col)"
                      autoFocus
                    />
                    <div class="quick-add-actions">
                      <button type="button" class="btn btn-primary btn-sm" [disabled]="!quickTaskTitle().trim()" (click)="onCreateQuickTask(col)">
                        Aggiungi
                      </button>
                      <button type="button" class="btn btn-secondary btn-sm" (click)="cancelQuickAdd()">
                        Annulla
                      </button>
                    </div>
                  </div>
                } @else {
                  <button class="btn-quick-add" (click)="startQuickAdd(col.id)">
                    + Aggiungi Task
                  </button>
                }
              </div>
            </div>
          }

          <!-- Quick Add Column Card in Canvas -->
          <div class="add-column-column">
            <button type="button" class="btn-canvas-add-col" (click)="openCreateColumnModal()">
              <span class="plus-icon">+</span>
              <span>Aggiungi Colonna</span>
            </button>
          </div>
        </div>
      }

      <!-- Task Detail Modal Dialog -->
      <app-task-detail-modal
        [isOpen]="isTaskModalOpen()"
        [taskId]="selectedTaskId()"
        (taskUpdated)="onReloadBoard()"
        (close)="closeTaskModal()"
      ></app-task-detail-modal>

      <!-- Create Column Modal Dialog -->
      <app-modal
        [isOpen]="isCreateColumnModalOpen()"
        [title]="'Aggiungi Nuova Colonna'"
        (close)="closeCreateColumnModal()"
      >
        <form [formGroup]="columnForm" (ngSubmit)="onCreateColumn()">
          @if (columnErrorMessage()) {
            <div class="alert alert-danger">{{ columnErrorMessage() }}</div>
          }
          <div class="form-group">
            <label class="form-label" for="column-name">Nome Colonna</label>
            <input
              id="column-name"
              type="text"
              class="form-input"
              placeholder="es. Code Review, QA, Bloccati..."
              formControlName="name"
            />
            @if (columnForm.get('name')?.invalid && columnForm.get('name')?.touched) {
              <span class="form-error">Il nome della colonna è obbligatorio.</span>
            }
          </div>

          <div class="form-group">
            <label class="form-label" for="column-status">Stato di Destinazione Task</label>
            <select id="column-status" class="form-select" formControlName="targetStatus">
              <option [value]="TaskItemStatus.Backlog">Arretrato (Backlog)</option>
              <option [value]="TaskItemStatus.Todo">Da Fare (Todo)</option>
              <option [value]="TaskItemStatus.InProgress">In Corso (InProgress)</option>
              <option [value]="TaskItemStatus.InReview">In Revisione (InReview)</option>
              <option [value]="TaskItemStatus.Done">Completato (Done)</option>
            </select>
            <span class="form-hint">I task assegnati o spostati in questa colonna acquisiranno questo stato.</span>
          </div>

          <div class="modal-actions">
            <button type="button" class="btn btn-secondary" (click)="closeCreateColumnModal()">Annulla</button>
            <button type="submit" class="btn btn-primary" [disabled]="columnForm.invalid || isSubmittingColumn()">
              {{ isSubmittingColumn() ? 'Aggiunta in corso...' : 'Aggiungi Colonna' }}
            </button>
          </div>
        </form>
      </app-modal>

      <!-- Delete Column Confirmation Modal -->
      <app-modal
        [isOpen]="!!columnToDelete()"
        [title]="'Elimina Colonna'"
        (close)="cancelDeleteColumn()"
      >
        <div class="confirm-modal-body">
          <p>Sei sicuro di voler eliminare la colonna <strong>"{{ columnToDelete()?.name }}"</strong>?</p>
          @if ((columnToDelete()?.tasks?.length || 0) > 0) {
            <div class="alert alert-warning">
              <span>⚠️ Questa colonna contiene <strong>{{ columnToDelete()?.tasks?.length }}</strong> task. L'eliminazione comporterà la rimozione della colonna.</span>
            </div>
          }
          <div class="modal-actions">
            <button type="button" class="btn btn-secondary" (click)="cancelDeleteColumn()">Annulla</button>
            <button type="button" class="btn btn-danger" [disabled]="isDeletingColumn()" (click)="onDeleteColumn()">
              {{ isDeletingColumn() ? 'Eliminazione...' : 'Elimina Colonna' }}
            </button>
          </div>
        </div>
      </app-modal>

      <!-- Create Board Modal Dialog -->
      <app-modal
        [isOpen]="isCreateBoardModalOpen()"
        [title]="'Crea Nuova Board Kanban'"
        (close)="closeCreateBoardModal()"
      >
        <form [formGroup]="boardForm" (ngSubmit)="onCreateBoard()">
          @if (boardErrorMessage()) {
            <div class="alert alert-danger">{{ boardErrorMessage() }}</div>
          }
          <div class="form-group">
            <label class="form-label" for="board-modal-name">Nome della Board</label>
            <input
              id="board-modal-name"
              type="text"
              class="form-input"
              placeholder="es. Sprint Attivo / Backlog"
              formControlName="name"
            />
            @if (boardForm.get('name')?.invalid && boardForm.get('name')?.touched) {
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
            <button type="button" class="btn btn-secondary" (click)="closeCreateBoardModal()">Annulla</button>
            <button type="submit" class="btn btn-primary" [disabled]="boardForm.invalid || isSubmittingBoard()">
              {{ isSubmittingBoard() ? 'Creazione in corso...' : 'Crea Board' }}
            </button>
          </div>
        </form>
      </app-modal>
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

    .breadcrumb-link:hover {
      text-decoration: underline;
    }

    .board-header {
      margin-bottom: 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 0.75rem;
    }

    .board-header-top {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 1rem;
      flex-wrap: wrap;
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

    .board-header-actions {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .board-nav-bar {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      overflow-x: auto;
      padding: 0.25rem 0;
    }

    .board-nav-label {
      font-size: 0.8125rem;
      font-weight: 600;
      color: var(--text-secondary);
      white-space: nowrap;
    }

    .board-pills-container {
      display: flex;
      align-items: center;
      gap: 0.35rem;
      flex-wrap: wrap;
    }

    .board-pill {
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
      padding: 0.25rem 0.65rem;
      border-radius: 9999px;
      font-size: 0.8rem;
      font-weight: 500;
      background-color: var(--bg-surface);
      border: 1px solid var(--border-color);
      color: var(--text-secondary);
      cursor: pointer;
      transition: all 0.15s ease;
      white-space: nowrap;
    }

    .board-pill:hover {
      border-color: var(--color-primary-border);
      color: var(--color-primary);
      background-color: var(--color-primary-light);
    }

    .board-pill.active {
      background-color: var(--color-primary);
      border-color: var(--color-primary);
      color: #ffffff;
      font-weight: 600;
    }

    .board-select-wrapper {
      display: none;
    }

    @media (max-width: 640px) {
      .board-pills-container {
        display: none;
      }
      .board-select-wrapper {
        display: block;
        min-width: 140px;
      }
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
      max-height: calc(100vh - 190px);
      display: flex;
      flex-direction: column;
      background-color: var(--bg-subtle);
      border: 1px solid var(--border-color);
      border-radius: var(--radius-lg);
      padding: 0.75rem;
    }

    .column-header {
      padding: 0.25rem 0.25rem 0.75rem;
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 0.5rem;
    }

    .column-title-group {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      overflow: hidden;
    }

    .column-title {
      font-weight: 600;
      font-size: 0.95rem;
      color: var(--text-main);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .column-count {
      background-color: var(--bg-surface);
      border: 1px solid var(--border-color);
      color: var(--text-secondary);
      font-size: 0.75rem;
      font-weight: 600;
      padding: 0.1rem 0.45rem;
      border-radius: 9999px;
      flex-shrink: 0;
    }

    .column-actions {
      display: flex;
      align-items: center;
      gap: 0.15rem;
      flex-shrink: 0;
    }

    .btn-col-action {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 24px;
      height: 24px;
      padding: 0;
      border-radius: var(--radius-sm);
      border: 1px solid transparent;
      background: none;
      color: var(--text-muted);
      cursor: pointer;
      font-size: 0.85rem;
      transition: all 0.15s ease;
    }

    .btn-col-action:hover:not(:disabled) {
      background-color: var(--bg-surface);
      color: var(--text-main);
      border-color: var(--border-color);
    }

    .btn-col-action:disabled {
      opacity: 0.25;
      cursor: not-allowed;
    }

    .btn-delete-col:hover:not(:disabled) {
      background-color: var(--color-danger-bg);
      color: var(--color-danger);
      border-color: var(--color-danger-border);
    }

    .add-column-column {
      min-width: 260px;
      width: 260px;
      flex-shrink: 0;
    }

    .btn-canvas-add-col {
      width: 100%;
      height: 48px;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      border: 1px dashed var(--border-color);
      border-radius: var(--radius-lg);
      background-color: rgba(241, 245, 249, 0.7);
      color: var(--text-secondary);
      font-weight: 500;
      font-size: 0.875rem;
      cursor: pointer;
      transition: all 0.15s ease;
    }

    .btn-canvas-add-col:hover {
      background-color: var(--bg-surface);
      border-color: var(--color-primary);
      color: var(--color-primary);
    }

    .plus-icon {
      font-size: 1.15rem;
      line-height: 1;
      font-weight: 600;
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

    .form-hint {
      font-size: 0.75rem;
      color: var(--text-muted);
      margin-top: 0.25rem;
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
      margin-top: 1.25rem;
    }

    .confirm-modal-body {
      display: flex;
      flex-direction: column;
      gap: 1rem;
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
export class KanbanBoardComponent implements OnInit, OnDestroy {
  readonly boardService = inject(BoardService);
  private readonly taskService = inject(TaskService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  private routeSub?: Subscription;

  readonly boardId = signal<string>('');
  readonly projectBoards = signal<BoardDto[]>([]);
  readonly isTaskModalOpen = signal(false);
  readonly selectedTaskId = signal<string | null>(null);

  readonly activeAddColumnId = signal<string | null>(null);
  readonly quickTaskTitle = signal<string>('');

  // Column Creation Modal State
  readonly isCreateColumnModalOpen = signal(false);
  readonly isSubmittingColumn = signal(false);
  readonly columnErrorMessage = signal<string | null>(null);

  // Column Deletion Confirmation Modal State
  readonly columnToDelete = signal<BoardColumnDetailDto | null>(null);
  readonly isDeletingColumn = signal(false);

  // Board Creation Modal State
  readonly isCreateBoardModalOpen = signal(false);
  readonly isSubmittingBoard = signal(false);
  readonly boardErrorMessage = signal<string | null>(null);

  readonly TaskItemStatus = TaskItemStatus;

  readonly columnForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    targetStatus: [TaskItemStatus.Todo, [Validators.required]]
  });

  readonly boardForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    createDefaultColumns: [true]
  });

  ngOnInit(): void {
    this.routeSub = this.route.paramMap.subscribe(params => {
      const id = params.get('boardId');
      if (id && id !== this.boardId()) {
        this.boardId.set(id);
        this.loadBoard(id);
      }
    });
  }

  ngOnDestroy(): void {
    this.routeSub?.unsubscribe();
  }

  loadBoard(id: string): void {
    this.boardService.getBoardDetails(id).subscribe({
      next: (board) => {
        if (board?.projectId) {
          this.loadProjectBoards(board.projectId);
        }
      }
    });
  }

  loadProjectBoards(projectId: string): void {
    this.boardService.getBoardsByProject(projectId).subscribe({
      next: (boards) => this.projectBoards.set(boards)
    });
  }

  onReloadBoard(): void {
    const id = this.boardId();
    if (id) {
      this.loadBoard(id);
    }
  }

  onSelectBoard(targetBoardId: string): void {
    if (targetBoardId !== this.boardId()) {
      this.router.navigate(['/boards', targetBoardId]);
    }
  }

  onBoardSelectChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    if (target && target.value && target.value !== this.boardId()) {
      this.router.navigate(['/boards', target.value]);
    }
  }

  // Column Reordering Methods
  onMoveColumnLeft(col: BoardColumnDetailDto, index: number): void {
    const board = this.boardService.currentBoard();
    if (!board || index <= 0) return;

    const columns = [...board.columns];
    const temp = columns[index];
    columns[index] = columns[index - 1];
    columns[index - 1] = temp;

    const reorderRequest: ReorderColumnsRequest = {
      columns: columns.map((c, i) => ({
        columnId: c.id,
        order: i
      }))
    };

    this.boardService.reorderColumns(board.id, reorderRequest).subscribe();
  }

  onMoveColumnRight(col: BoardColumnDetailDto, index: number): void {
    const board = this.boardService.currentBoard();
    if (!board || index >= board.columns.length - 1) return;

    const columns = [...board.columns];
    const temp = columns[index];
    columns[index] = columns[index + 1];
    columns[index + 1] = temp;

    const reorderRequest: ReorderColumnsRequest = {
      columns: columns.map((c, i) => ({
        columnId: c.id,
        order: i
      }))
    };

    this.boardService.reorderColumns(board.id, reorderRequest).subscribe();
  }

  // Column Creation Methods
  openCreateColumnModal(): void {
    this.columnForm.reset({
      name: '',
      targetStatus: TaskItemStatus.Todo
    });
    this.columnErrorMessage.set(null);
    this.isCreateColumnModalOpen.set(true);
  }

  closeCreateColumnModal(): void {
    this.isCreateColumnModalOpen.set(false);
  }

  onCreateColumn(): void {
    if (this.columnForm.invalid) {
      this.columnForm.markAllAsTouched();
      return;
    }
    const currentId = this.boardId();
    if (!currentId) return;

    this.isSubmittingColumn.set(true);
    this.columnErrorMessage.set(null);

    const { name, targetStatus } = this.columnForm.getRawValue();

    this.boardService.createColumn(currentId, {
      name: name.trim(),
      targetStatus: Number(targetStatus)
    }).subscribe({
      next: () => {
        this.isSubmittingColumn.set(false);
        this.closeCreateColumnModal();
      },
      error: (err) => {
        this.isSubmittingColumn.set(false);
        const msg = err.error?.detail || err.error?.message || 'Errore durante la creazione della colonna.';
        this.columnErrorMessage.set(msg);
      }
    });
  }

  // Column Deletion Methods
  confirmDeleteColumn(col: BoardColumnDetailDto): void {
    this.columnToDelete.set(col);
  }

  cancelDeleteColumn(): void {
    this.columnToDelete.set(null);
  }

  onDeleteColumn(): void {
    const col = this.columnToDelete();
    if (!col) return;

    this.isDeletingColumn.set(true);
    this.boardService.deleteColumn(col.id).subscribe({
      next: () => {
        this.isDeletingColumn.set(false);
        this.columnToDelete.set(null);
      },
      error: () => {
        this.isDeletingColumn.set(false);
        this.columnToDelete.set(null);
      }
    });
  }

  // Board Creation Methods
  openCreateBoardModal(): void {
    this.boardForm.reset({
      name: '',
      createDefaultColumns: true
    });
    this.boardErrorMessage.set(null);
    this.isCreateBoardModalOpen.set(true);
  }

  closeCreateBoardModal(): void {
    this.isCreateBoardModalOpen.set(false);
  }

  onCreateBoard(): void {
    if (this.boardForm.invalid) {
      this.boardForm.markAllAsTouched();
      return;
    }
    const current = this.boardService.currentBoard();
    if (!current?.projectId) return;

    this.isSubmittingBoard.set(true);
    this.boardErrorMessage.set(null);

    const req = {
      name: this.boardForm.value.name!.trim(),
      createDefaultColumns: this.boardForm.value.createDefaultColumns ?? true
    };

    this.boardService.createBoard(current.projectId, req).subscribe({
      next: (newBoard) => {
        this.isSubmittingBoard.set(false);
        this.closeCreateBoardModal();
        this.projectBoards.update(boards => [...boards, newBoard]);
        this.router.navigate(['/boards', newBoard.id]);
      },
      error: (err) => {
        this.isSubmittingBoard.set(false);
        const msg = err.error?.detail || err.error?.message || 'Errore durante la creazione della board.';
        this.boardErrorMessage.set(msg);
      }
    });
  }

  // Task Drag & Drop
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
