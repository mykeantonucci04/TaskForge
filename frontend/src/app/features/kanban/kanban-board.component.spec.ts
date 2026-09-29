import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router, ActivatedRoute } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of } from 'rxjs';
import { KanbanBoardComponent } from './kanban-board.component';
import { BoardService } from '../../core/services/board.service';
import { TaskService } from '../../core/services/task.service';
import { BoardDetailDto, BoardDto, BoardColumnDto, TaskItemStatus } from '../../core/models/board.models';

describe('KanbanBoardComponent', () => {
  let component: KanbanBoardComponent;
  let fixture: ComponentFixture<KanbanBoardComponent>;
  let boardService: BoardService;
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
});
