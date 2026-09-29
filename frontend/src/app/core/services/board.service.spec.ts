import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { BoardService } from './board.service';
import { environment } from '../../../environments/environment';
import {
  BoardDetailDto,
  BoardDto,
  BoardColumnDto,
  TaskItemStatus,
  ReorderColumnsRequest,
  CreateBoardColumnRequest,
  CreateBoardRequest
} from '../models/board.models';

describe('BoardService', () => {
  let service: BoardService;
  let httpTesting: HttpTestingController;

  const mockBoardDetail: BoardDetailDto = {
    id: 'board-1',
    tenantId: 'tenant-1',
    projectId: 'project-1',
    name: 'Main Board',
    createdAtUtc: new Date().toISOString(),
    columns: [
      {
        id: 'col-1',
        boardId: 'board-1',
        name: 'To Do',
        order: 0,
        targetStatus: TaskItemStatus.Todo,
        tasks: []
      },
      {
        id: 'col-2',
        boardId: 'board-1',
        name: 'In Progress',
        order: 1,
        targetStatus: TaskItemStatus.InProgress,
        tasks: []
      },
      {
        id: 'col-3',
        boardId: 'board-1',
        name: 'Done',
        order: 2,
        targetStatus: TaskItemStatus.Done,
        tasks: []
      }
    ]
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        BoardService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(BoardService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should load board details and update currentBoard signal', () => {
    expect(service.currentBoard()).toBeNull();

    service.getBoardDetails('board-1').subscribe(detail => {
      expect(detail.id).toBe('board-1');
      expect(detail.columns.length).toBe(3);
      expect(service.currentBoard()?.id).toBe('board-1');
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/boards/board-1`);
    expect(req.request.method).toBe('GET');
    req.flush(mockBoardDetail);
  });

  it('should get boards by project', () => {
    const mockBoards: BoardDto[] = [
      {
        id: 'board-1',
        tenantId: 'tenant-1',
        projectId: 'project-1',
        name: 'Main Board',
        createdAtUtc: new Date().toISOString()
      },
      {
        id: 'board-2',
        tenantId: 'tenant-1',
        projectId: 'project-1',
        name: 'Secondary Board',
        createdAtUtc: new Date().toISOString()
      }
    ];

    service.getBoardsByProject('project-1').subscribe(boards => {
      expect(boards.length).toBe(2);
      expect(boards[0].name).toBe('Main Board');
      expect(boards[1].name).toBe('Secondary Board');
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/projects/project-1/boards`);
    expect(req.request.method).toBe('GET');
    req.flush(mockBoards);
  });

  it('should create a board in a project', () => {
    const newBoardReq: CreateBoardRequest = {
      name: 'Sprint 2 Board',
      createDefaultColumns: true
    };

    const createdBoard: BoardDto = {
      id: 'board-new',
      tenantId: 'tenant-1',
      projectId: 'project-1',
      name: 'Sprint 2 Board',
      createdAtUtc: new Date().toISOString()
    };

    service.createBoard('project-1', newBoardReq).subscribe(board => {
      expect(board.id).toBe('board-new');
      expect(board.name).toBe('Sprint 2 Board');
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/projects/project-1/boards`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(newBoardReq);
    req.flush(createdBoard);
  });

  it('should create a column and append to currentBoard signal', () => {
    // Seed currentBoard
    service.currentBoard.set({
      ...mockBoardDetail,
      columns: [...mockBoardDetail.columns]
    });

    const createColReq: CreateBoardColumnRequest = {
      name: 'Testing QA',
      targetStatus: TaskItemStatus.InReview
    };

    const createdCol: BoardColumnDto = {
      id: 'col-4',
      boardId: 'board-1',
      name: 'Testing QA',
      order: 3,
      targetStatus: TaskItemStatus.InReview
    };

    service.createColumn('board-1', createColReq).subscribe(col => {
      expect(col.id).toBe('col-4');
      const board = service.currentBoard();
      expect(board?.columns.length).toBe(4);
      expect(board?.columns.some(c => c.id === 'col-4')).toBe(true);
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/boards/board-1/columns`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(createColReq);
    req.flush(createdCol);
  });

  it('should reorder columns and update currentBoard signal', () => {
    // Seed currentBoard with original order: col-1 (0), col-2 (1), col-3 (2)
    service.currentBoard.set({
      ...mockBoardDetail,
      columns: [
        { ...mockBoardDetail.columns[0], order: 0 },
        { ...mockBoardDetail.columns[1], order: 1 },
        { ...mockBoardDetail.columns[2], order: 2 }
      ]
    });

    // Reorder: col-2 becomes 0, col-1 becomes 1, col-3 remains 2
    const reorderReq: ReorderColumnsRequest = {
      columns: [
        { columnId: 'col-2', order: 0 },
        { columnId: 'col-1', order: 1 },
        { columnId: 'col-3', order: 2 }
      ]
    };

    service.reorderColumns('board-1', reorderReq).subscribe(success => {
      expect(success).toBe(true);
      const board = service.currentBoard();
      expect(board?.columns.length).toBe(3);
      expect(board?.columns[0].id).toBe('col-2');
      expect(board?.columns[0].order).toBe(0);
      expect(board?.columns[1].id).toBe('col-1');
      expect(board?.columns[1].order).toBe(1);
      expect(board?.columns[2].id).toBe('col-3');
      expect(board?.columns[2].order).toBe(2);
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/boards/board-1/columns/reorder`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(reorderReq);
    req.flush(true);
  });

  it('should delete a column and remove it from currentBoard signal', () => {
    // Seed currentBoard
    service.currentBoard.set({
      ...mockBoardDetail,
      columns: [...mockBoardDetail.columns]
    });

    expect(service.currentBoard()?.columns.length).toBe(3);

    service.deleteColumn('col-2').subscribe(success => {
      expect(success).toBe(true);
      const board = service.currentBoard();
      expect(board?.columns.length).toBe(2);
      expect(board?.columns.some(c => c.id === 'col-2')).toBe(false);
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/columns/col-2`);
    expect(req.request.method).toBe('DELETE');
    req.flush(true);
  });
});
