import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router, ActivatedRoute } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { KanbanBoardComponent } from './kanban-board.component';
import { BoardService } from '../../core/services/board.service';
import { TaskService } from '../../core/services/task.service';
import { WorkspaceService } from '../../core/services/workspace.service';
import { ProjectService } from '../../core/services/project.service';
import { BoardDetailDto, BoardDto, BoardColumnDto, TaskItemStatus } from '../../core/models/board.models';
import { LabelDto, TaskDto, TaskPriority } from '../../core/models/task.models';

describe('KanbanBoardComponent', () => {
  let component: KanbanBoardComponent;
  let fixture: ComponentFixture<KanbanBoardComponent>;
  let boardService: BoardService;
  let taskService: TaskService;
  let workspaceService: WorkspaceService;
  let projectService: ProjectService;
  let router: Router;

  const mockBoardDetail: BoardDetailDto = {
    id: 'b-1',
    tenantId: 't-1',
    projectId: 'p-1',
    name: 'Dev Board',
    createdAtUtc: new Date().toISOString(),
    columns: [
      {
        id: 'c-1',
        boardId: 'b-1',
        name: 'To Do',
        order: 0,
        targetStatus: TaskItemStatus.Todo,
        tasks: []
      },
      {
        id: 'c-2',
        boardId: 'b-1',
        name: 'In Progress',
        order: 1,
        targetStatus: TaskItemStatus.InProgress,
        tasks: []
      },
      {
        id: 'c-3',
        boardId: 'b-1',
        name: 'Done',
        order: 2,
        targetStatus: TaskItemStatus.Done,
        tasks: []
      }
    ]
  };

  const mockBoards: BoardDto[] = [
    {
      id: 'b-1',
      tenantId: 't-1',
      projectId: 'p-1',
      name: 'Dev Board',
      createdAtUtc: new Date().toISOString()
    },
    {
      id: 'b-2',
      tenantId: 't-1',
      projectId: 'p-1',
      name: 'Release Board',
      createdAtUtc: new Date().toISOString()
    }
  ];

  const mockBoardWithTasks: BoardDetailDto = {
    id: 'b-1',
    tenantId: 't-1',
    projectId: 'p-1',
    name: 'Dev Board',
    createdAtUtc: new Date().toISOString(),
    columns: [
      {
        id: 'c-1',
        boardId: 'b-1',
        name: 'To Do',
        order: 0,
        targetStatus: TaskItemStatus.Todo,
        tasks: [
          {
            id: 't-1',
            columnId: 'c-1',
            title: 'Fix authentication bug',
            description: 'Resolve 401 issue with expired tokens',
            priority: TaskPriority.Urgent,
            status: TaskItemStatus.Todo,
            position: 1000,
            createdAtUtc: new Date().toISOString(),
            labels: [{ id: 'lbl-1', workspaceId: 'ws-1', name: 'Bug', colorHex: '#ef4444' }]
          },
          {
            id: 't-2',
            columnId: 'c-1',
            title: 'Add search filter',
            description: 'Implement filter toolbar above kanban',
            priority: TaskPriority.High,
            status: TaskItemStatus.Todo,
            position: 2000,
            createdAtUtc: new Date().toISOString(),
            labels: [{ id: 'lbl-2', workspaceId: 'ws-1', name: 'Feature', colorHex: '#3b82f6' }]
          }
        ]
      },
      {
        id: 'c-2',
        boardId: 'b-1',
        name: 'In Progress',
        order: 1,
        targetStatus: TaskItemStatus.InProgress,
        tasks: [
          {
            id: 't-3',
            columnId: 'c-2',
            title: 'Documentation update',
            description: 'Write README and API docs',
            priority: TaskPriority.Low,
            status: TaskItemStatus.InProgress,
            position: 1000,
            createdAtUtc: new Date().toISOString(),
            labels: []
          }
        ]
      },
      {
        id: 'c-3',
        boardId: 'b-1',
        name: 'Done',
        order: 2,
        targetStatus: TaskItemStatus.Done,
        tasks: []
      }
    ]
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [KanbanBoardComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          { path: 'boards/:boardId', component: KanbanBoardComponent }
        ]),
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of({
              get: (key: string) => (key === 'boardId' ? 'b-1' : null)
            })
          }
        }
      ]
    }).compileComponents();

    boardService = TestBed.inject(BoardService);
    taskService = TestBed.inject(TaskService);
    workspaceService = TestBed.inject(WorkspaceService);
    projectService = TestBed.inject(ProjectService);
    router = TestBed.inject(Router);

    vi.spyOn(boardService, 'getBoardDetails').mockReturnValue(of(mockBoardDetail));
    vi.spyOn(boardService, 'getBoardsByProject').mockReturnValue(of(mockBoards));

    fixture = TestBed.createComponent(KanbanBoardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load board details and project boards on init', () => {
    expect(component).toBeTruthy();
    expect(boardService.getBoardDetails).toHaveBeenCalledWith('b-1');
    expect(boardService.getBoardsByProject).toHaveBeenCalledWith('p-1');
    expect(component.projectBoards().length).toBe(2);
  });

  it('should reorder columns when moving column right', () => {
    boardService.currentBoard.set(mockBoardDetail);
    const reorderSpy = vi.spyOn(boardService, 'reorderColumns').mockReturnValue(of(true));

    component.onMoveColumnRight(mockBoardDetail.columns[0], 0);

    expect(reorderSpy).toHaveBeenCalledWith('b-1', {
      columns: [
        { columnId: 'c-2', order: 0 },
        { columnId: 'c-1', order: 1 },
        { columnId: 'c-3', order: 2 }
      ]
    });
  });

  it('should reorder columns when moving column left', () => {
    boardService.currentBoard.set(mockBoardDetail);
    const reorderSpy = vi.spyOn(boardService, 'reorderColumns').mockReturnValue(of(true));

    component.onMoveColumnLeft(mockBoardDetail.columns[1], 1);

    expect(reorderSpy).toHaveBeenCalledWith('b-1', {
      columns: [
        { columnId: 'c-2', order: 0 },
        { columnId: 'c-1', order: 1 },
        { columnId: 'c-3', order: 2 }
      ]
    });
  });

  it('should not reorder when moving leftmost column left', () => {
    boardService.currentBoard.set(mockBoardDetail);
    const reorderSpy = vi.spyOn(boardService, 'reorderColumns');

    component.onMoveColumnLeft(mockBoardDetail.columns[0], 0);
    expect(reorderSpy).not.toHaveBeenCalled();
  });

  it('should not reorder when moving rightmost column right', () => {
    boardService.currentBoard.set(mockBoardDetail);
    const reorderSpy = vi.spyOn(boardService, 'reorderColumns');

    component.onMoveColumnRight(mockBoardDetail.columns[2], 2);
    expect(reorderSpy).not.toHaveBeenCalled();
  });

  it('should create a column via onCreateColumn', () => {
    boardService.currentBoard.set(mockBoardDetail);
    const mockCreatedCol: BoardColumnDto = {
      id: 'c-4',
      boardId: 'b-1',
      name: 'QA Review',
      order: 3,
      targetStatus: TaskItemStatus.InReview
    };

    const createColSpy = vi.spyOn(boardService, 'createColumn').mockReturnValue(of(mockCreatedCol));

    component.openCreateColumnModal();
    component.columnForm.setValue({
      name: 'QA Review',
      targetStatus: TaskItemStatus.InReview
    });

    component.onCreateColumn();

    expect(createColSpy).toHaveBeenCalledWith('b-1', {
      name: 'QA Review',
      targetStatus: TaskItemStatus.InReview
    });
    expect(component.isCreateColumnModalOpen()).toBe(false);
  });

  it('should delete a column via confirm and onDeleteColumn', () => {
    boardService.currentBoard.set(mockBoardDetail);
    const deleteSpy = vi.spyOn(boardService, 'deleteColumn').mockReturnValue(of(true));

    component.confirmDeleteColumn(mockBoardDetail.columns[1]);
    expect(component.columnToDelete()?.id).toBe('c-2');

    component.onDeleteColumn();

    expect(deleteSpy).toHaveBeenCalledWith('c-2');
    expect(component.columnToDelete()).toBeNull();
  });

  it('should create a board and navigate to it via onCreateBoard', () => {
    boardService.currentBoard.set(mockBoardDetail);
    const navigateSpy = vi.spyOn(router, 'navigate');

    const createdBoard: BoardDto = {
      id: 'b-new',
      tenantId: 't-1',
      projectId: 'p-1',
      name: 'Sprint 3',
      createdAtUtc: new Date().toISOString()
    };

    const createBoardSpy = vi.spyOn(boardService, 'createBoard').mockReturnValue(of(createdBoard));

    component.openCreateBoardModal();
    component.boardForm.setValue({
      name: 'Sprint 3',
      createDefaultColumns: true
    });

    component.onCreateBoard();

    expect(createBoardSpy).toHaveBeenCalledWith('p-1', {
      name: 'Sprint 3',
      createDefaultColumns: true
    });
    expect(component.isCreateBoardModalOpen()).toBe(false);
    expect(navigateSpy).toHaveBeenCalledWith(['/boards', 'b-new']);
  });

  it('should navigate when selecting another board from switcher', () => {
    const navigateSpy = vi.spyOn(router, 'navigate');
    component.onSelectBoard('b-2');
    expect(navigateSpy).toHaveBeenCalledWith(['/boards', 'b-2']);
  });

  it('should open edit column modal with prefilled data and close it', () => {
    const targetCol = mockBoardDetail.columns[1];
    component.openEditColumnModal(targetCol);

    expect(component.columnToEdit()?.id).toBe('c-2');
    expect(component.isEditColumnModalOpen()).toBe(true);
    expect(component.editColumnForm.value.name).toBe('In Progress');
    expect(component.editColumnForm.value.targetStatus).toBe(TaskItemStatus.InProgress);
    expect(component.editColumnErrorMessage()).toBeNull();

    component.closeEditColumnModal();
    expect(component.columnToEdit()).toBeNull();
    expect(component.isEditColumnModalOpen()).toBe(false);
  });

  it('should update a column via onUpdateColumn and close modal on success', () => {
    const targetCol = mockBoardDetail.columns[0];
    const mockUpdatedCol: BoardColumnDto = {
      id: 'c-1',
      boardId: 'b-1',
      name: 'Ready for Dev',
      order: 0,
      targetStatus: TaskItemStatus.Todo
    };

    const updateColSpy = vi.spyOn(boardService, 'updateColumn').mockReturnValue(of(mockUpdatedCol));

    component.openEditColumnModal(targetCol);
    component.editColumnForm.setValue({
      name: '  Ready for Dev  ',
      targetStatus: TaskItemStatus.Todo
    });

    component.onUpdateColumn();

    expect(updateColSpy).toHaveBeenCalledWith('c-1', {
      name: 'Ready for Dev',
      order: 0,
      targetStatus: TaskItemStatus.Todo
    });
    expect(component.isSubmittingEditColumn()).toBe(false);
    expect(component.isEditColumnModalOpen()).toBe(false);
    expect(component.columnToEdit()).toBeNull();
  });

  it('should not submit onUpdateColumn if edit form is invalid', () => {
    const updateColSpy = vi.spyOn(boardService, 'updateColumn');
    const targetCol = mockBoardDetail.columns[0];

    component.openEditColumnModal(targetCol);
    component.editColumnForm.setValue({
      name: '',
      targetStatus: TaskItemStatus.Todo
    });

    component.onUpdateColumn();

    expect(updateColSpy).not.toHaveBeenCalled();
    expect(component.editColumnForm.invalid).toBe(true);
    expect(component.isEditColumnModalOpen()).toBe(true);
  });

  it('should handle error when updating column fails', () => {
    const targetCol = mockBoardDetail.columns[0];
    const errorResponse = {
      error: {
        detail: 'Nome colonna già esistente'
      }
    };

    vi.spyOn(boardService, 'updateColumn').mockReturnValue(throwError(() => errorResponse));

    component.openEditColumnModal(targetCol);
    component.editColumnForm.setValue({
      name: 'Duplicate Column',
      targetStatus: TaskItemStatus.Todo
    });

    component.onUpdateColumn();

    expect(component.isSubmittingEditColumn()).toBe(false);
    expect(component.editColumnErrorMessage()).toBe('Nome colonna già esistente');
    expect(component.isEditColumnModalOpen()).toBe(true);
  });

  describe('Kanban Search & Filter Toolbar Integration', () => {
    it('should return all columns and tasks when no filter criteria are applied', () => {
      boardService.currentBoard.set(mockBoardWithTasks);
      const cols = component.filteredColumns();

      expect(cols.length).toBe(3);
      expect(cols[0].tasks.length).toBe(2);
      expect(cols[1].tasks.length).toBe(1);
      expect(cols[2].tasks.length).toBe(0);
    });

    it('should filter tasks by search query matching title (case-insensitive)', () => {
      boardService.currentBoard.set(mockBoardWithTasks);
      component.searchQuery.set('auth');
      const cols = component.filteredColumns();

      expect(cols.length).toBe(3);
      expect(cols[0].tasks.map(t => t.id)).toEqual(['t-1']);
      expect(cols[1].tasks.length).toBe(0);
      expect(cols[2].tasks.length).toBe(0);
    });

    it('should filter tasks by search query matching description (case-insensitive)', () => {
      boardService.currentBoard.set(mockBoardWithTasks);
      component.searchQuery.set('README');
      const cols = component.filteredColumns();

      expect(cols.length).toBe(3);
      expect(cols[0].tasks.length).toBe(0);
      expect(cols[1].tasks.map(t => t.id)).toEqual(['t-3']);
      expect(cols[2].tasks.length).toBe(0);
    });

    it('should filter tasks by priority', () => {
      boardService.currentBoard.set(mockBoardWithTasks);
      component.selectedPriority.set(TaskPriority.High);
      const cols = component.filteredColumns();

      expect(cols.length).toBe(3);
      expect(cols[0].tasks.map(t => t.id)).toEqual(['t-2']);
      expect(cols[1].tasks.length).toBe(0);
      expect(cols[2].tasks.length).toBe(0);
    });

    it('should filter tasks by labelId', () => {
      boardService.currentBoard.set(mockBoardWithTasks);
      component.selectedLabelId.set('lbl-1');
      const cols = component.filteredColumns();

      expect(cols.length).toBe(3);
      expect(cols[0].tasks.map(t => t.id)).toEqual(['t-1']);
      expect(cols[1].tasks.length).toBe(0);
      expect(cols[2].tasks.length).toBe(0);
    });

    it('should filter tasks by combined search, priority, and labelId criteria', () => {
      boardService.currentBoard.set(mockBoardWithTasks);
      component.searchQuery.set('filter');
      component.selectedPriority.set(TaskPriority.High);
      component.selectedLabelId.set('lbl-2');
      const cols = component.filteredColumns();

      expect(cols.length).toBe(3);
      expect(cols[0].tasks.map(t => t.id)).toEqual(['t-2']);
      expect(cols[1].tasks.length).toBe(0);
      expect(cols[2].tasks.length).toBe(0);
    });

    it('should preserve all columns in DOM even when matching task count is 0', () => {
      boardService.currentBoard.set(mockBoardWithTasks);
      component.searchQuery.set('nonexistent query xyz');
      const cols = component.filteredColumns();

      expect(cols.length).toBe(3);
      expect(cols[0].tasks.length).toBe(0);
      expect(cols[1].tasks.length).toBe(0);
      expect(cols[2].tasks.length).toBe(0);
    });

    it('should update filter signals via onFilterChange', () => {
      component.onFilterChange({
        search: 'urgent bug',
        priority: TaskPriority.Urgent,
        labelId: 'lbl-1'
      });

      expect(component.searchQuery()).toBe('urgent bug');
      expect(component.selectedPriority()).toBe(TaskPriority.Urgent);
      expect(component.selectedLabelId()).toBe('lbl-1');
    });

    it('should reset all filter signals via onResetFilters', () => {
      component.searchQuery.set('urgent bug');
      component.selectedPriority.set(TaskPriority.Urgent);
      component.selectedLabelId.set('lbl-1');

      component.onResetFilters();

      expect(component.searchQuery()).toBe('');
      expect(component.selectedPriority()).toBeNull();
      expect(component.selectedLabelId()).toBeNull();
    });

    it('should load workspace labels when selectedWorkspace is present', () => {
      const mockLabels: LabelDto[] = [
        { id: 'lbl-1', workspaceId: 'ws-1', name: 'Bug', colorHex: '#ff0000' }
      ];
      workspaceService.selectedWorkspace.set({
        id: 'ws-1',
        tenantId: 't-1',
        name: 'Workspace 1',
        slug: 'ws-1',
        createdAtUtc: ''
      });
      vi.spyOn(taskService, 'getWorkspaceLabels').mockReturnValue(of(mockLabels));

      component.loadWorkspaceLabels();

      expect(taskService.getWorkspaceLabels).toHaveBeenCalledWith('ws-1');
      expect(component.availableLabels()).toEqual(mockLabels);
    });

    it('should load workspace labels via projectService when selectedWorkspace is not set', () => {
      const mockLabels: LabelDto[] = [
        { id: 'lbl-2', workspaceId: 'ws-2', name: 'Feature', colorHex: '#00ff00' }
      ];
      workspaceService.selectedWorkspace.set(null);
      vi.spyOn(projectService, 'getProjectById').mockReturnValue(of({
        id: 'p-1',
        tenantId: 't-1',
        workspaceId: 'ws-2',
        name: 'Project 1',
        key: 'PRJ1',
        createdAtUtc: '',
        boards: []
      }));
      vi.spyOn(taskService, 'getWorkspaceLabels').mockReturnValue(of(mockLabels));

      component.loadWorkspaceLabels('p-1');

      expect(projectService.getProjectById).toHaveBeenCalledWith('p-1');
      expect(taskService.getWorkspaceLabels).toHaveBeenCalledWith('ws-2');
      expect(component.availableLabels()).toEqual(mockLabels);
    });

    it('should move task and update backend on onTaskDrop', () => {
      boardService.currentBoard.set(mockBoardWithTasks);
      const movedTask: TaskDto = { ...mockBoardWithTasks.columns[0].tasks[0] };
      const moveTaskSpy = vi.spyOn(taskService, 'moveTask').mockReturnValue(of({
        ...movedTask,
        position: 1500,
        columnId: 'c-2',
        status: TaskItemStatus.InProgress
      }));

      const mockDropEvent: any = {
        previousContainer: { data: [movedTask] },
        container: { data: [] },
        previousIndex: 0,
        currentIndex: 0
      };

      component.onTaskDrop(mockDropEvent, mockBoardWithTasks.columns[1]);

      expect(moveTaskSpy).toHaveBeenCalledWith('t-1', {
        targetColumnId: 'c-2',
        newPosition: 1000,
        newStatus: TaskItemStatus.InProgress
      });
      expect(movedTask.columnId).toBe('c-2');
      expect(movedTask.status).toBe(TaskItemStatus.InProgress);
      expect(movedTask.position).toBe(1500);
    });
  });
});
