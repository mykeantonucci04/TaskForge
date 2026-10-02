import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
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
import { TaskDto } from '../../core/models/task.models';

import { BreadcrumbComponent, BreadcrumbItem } from '../../shared/components/breadcrumb/breadcrumb.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';
import { ModalComponent } from '../../shared/components/modal/modal.component';
import { TaskDetailModalComponent } from '../task/task-detail-modal.component';
import { KanbanColumnComponent } from './components/kanban-column/kanban-column.component';

@Component({
  selector: 'app-kanban-board',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    DragDropModule,
    BreadcrumbComponent,
    LoadingSpinnerComponent,
    ConfirmDialogComponent,
    ModalComponent,
    TaskDetailModalComponent,
    KanbanColumnComponent
  ],
  templateUrl: './kanban-board.component.html',
  styleUrl: './kanban-board.component.css'
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

  readonly breadcrumbItems = computed<BreadcrumbItem[]>(() => {
    const board = this.boardService.currentBoard();
    const items: BreadcrumbItem[] = [
      { label: 'Workspaces', route: ['/workspaces'] }
    ];
    if (board?.projectId) {
      items.push({ label: 'Progetto', route: ['/projects', board.projectId] });
    }
    items.push({ label: board?.name || 'Lavagna Kanban' });
    return items;
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

  getDeleteColumnMessage(): string {
    const col = this.columnToDelete();
    return col ? 'Sei sicuro di voler eliminare la colonna <strong>"' + col.name + '"</strong>?' : '';
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
  }

  cancelQuickAdd(): void {
    this.activeAddColumnId.set(null);
  }

  onCreateQuickTask(columnId: string, title: string): void {
    this.taskService.createTask({
      columnId,
      title
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
}
